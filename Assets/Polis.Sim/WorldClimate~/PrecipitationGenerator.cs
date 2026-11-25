using System;

namespace Polis.Sim
{
    /// <summary>
    /// Builds a precipitation field, a precipitation index in [0, 1] derived from it on a fixed scale (an input
    /// to a Whittaker-style biome lookup), and the humidity of the air at ground level. Precipitation comes from the
    /// wind alone, with no noise of its own: a column of air crosses the map along each wind line, entering at some
    /// humidity. Air can hold less moisture the colder it is, so where the ground rises the air cools, more of its
    /// water condenses to cloud and part of the cloud falls as rain; where the ground falls, the cloud evaporates
    /// again. Land downwind of high ground is dry because the air has already rained out.
    ///
    /// The air above each vertex is an AirColumn: its water is shared by the air lifted over the terrain (see
    /// AirLiftModel), whose humidity decides the cloud and so the rain, and the air at ground level, whose room for
    /// more water limits how much of the rain the ground gives back. The ground only gives back rain that fell on it,
    /// so dry air crossing dry land stays dry, and rain on coasts and windward slopes carries moisture further inland.
    /// </summary>
    internal static class PrecipitationGenerator
    {
        public readonly struct Result
        {
            /// <summary>
            /// Precipitation at each vertex: the moisture that falls there per tile of the wind's travel, in units of
            /// what saturated air at AirColumn.ReferenceTemperature holds. A relative amount, not a depth.
            /// </summary>
            public readonly FloatMap Precipitation;
            /// <summary>Precipitation mapped to [0, 1] by a fixed scale (See <see cref="PrecipitationIndexOf"/>).</summary>
            public readonly FloatMap PrecipitationIndex;
            /// <summary>
            /// Relative humidity of the air at ground level at each vertex, in [0, 1]. It uses the ground's temperature,
            /// so cold ground reads more humid than warm ground under the same air; 1 means the ground is in cloud or fog.
            /// </summary>
            public readonly FloatMap SurfaceHumidity;

            public Result(FloatMap precipitation, FloatMap precipitationIndex, FloatMap surfaceHumidity)
            {
                Precipitation = precipitation;
                PrecipitationIndex = precipitationIndex;
                SurfaceHumidity = surfaceHumidity;
            }
        }

        /// <summary>
        /// Sweeps the wind across the map (see SweepWind). The terrain is blurred before and the results after, so
        /// independent wind lines and single-tile bumps leave no streaks or spots.
        /// </summary>
        /// <param name="heights">Vertex heights; the result has the same vertex layout.</param>
        /// <param name="maxHeight">The height that counts as full elevation for cooling (minimum 1).</param>
        /// <param name="temperature">Temperature at each vertex in degrees C, from TemperatureGenerator; it sets how humid the air at ground level is and how much rain the ground can give back.</param>
        /// <param name="temperatureSettings">The settings that made the temperature: the air's temperature follows the same sea level and cooling, without the noise.</param>
        public static Result Generate(HeightMap heights, int maxHeight, FloatMap temperature, TemperatureSettings temperatureSettings, PrecipitationSettings settings)
        {
            temperatureSettings = temperatureSettings.Validated();
            settings = settings.Validated();
            int smoothing = settings.RainShadowSmoothing;
            var ground = heights.ToFloatMap().Blurred(smoothing);
            var (rain, surfaceHumidity) = SweepWind(ground, maxHeight, temperature, temperatureSettings, settings);
            var precipitation = rain.Blurred(smoothing);
            return new Result(precipitation, PrecipitationIndexOf(precipitation, settings), surfaceHumidity.Blurred(smoothing));
        }

        /// <summary>
        /// Maps precipitation to [0, 1] on a scale that is the same for every map, with a smooth curve so heavy
        /// rain still reads wetter than moderate rain: 1 - e^(-precipitation / PrecipitationReference).
        /// </summary>
        private static FloatMap PrecipitationIndexOf(FloatMap precipitation, PrecipitationSettings settings)
        {
            var precipitationIndex = new FloatMap(precipitation.Size);
            for (int i = 0; i < precipitationIndex.Data.Length; i++)
                precipitationIndex.Data[i] = 1f - (float)Math.Exp(-Math.Max(0f, precipitation.Data[i]) / settings.PrecipitationReference);
            return precipitationIndex;
        }

        /// <summary>
        /// The wind's step from one vertex to the next. A diagonal step is longer than an axis-aligned one, so rates
        /// the settings give per tile of travel must be converted before the sweep applies them once per step: a
        /// share compounds over the step's length (ShareForStep), and an amount that fell during a step is spread
        /// over the tiles it crossed (AmountPerTile).
        /// </summary>
        private readonly struct WindStep
        {
            public readonly int Dx;
            public readonly int Dy;
            /// <summary>Distance covered by one step, in tiles.</summary>
            public readonly float LengthInTiles;

            public WindStep(CompassDirection windFrom)
            {
                var (dx, dy) = windFrom.Opposite().Step();
                Dx = dx;
                Dy = dy;
                LengthInTiles = windFrom.StepLength();
            }

            /// <summary>The share that applies over one step, given the share that applies over one tile of travel.</summary>
            public float ShareForStep(float sharePerTile) => 1f - (float)Math.Pow(1f - sharePerTile, LengthInTiles);

            /// <summary>The amount per tile of travel, given the amount that applied over one step.</summary>
            public float AmountPerTile(float amountInStep) => amountInStep / LengthInTiles;
        }

        /// <summary>
        /// The height the air has been lifted to above each vertex, which sets how cold it is. Wind does not hug the
        /// ground: it is lifted ahead of rising ground and stays cold for a while after the ground drops, so rain
        /// builds on the windward foothills before the crest and the rain shadow begins behind it instead of at a
        /// sharp line. The lifted height is a memory of past lifting, not a gap between the air and the ground.
        /// </summary>
        private readonly struct AirLiftModel
        {
            private readonly WindStep step;
            private readonly int anticipationSteps;
            private readonly float liftBeforeRidge;
            private readonly float riseShare;
            private readonly float sinkShare;

            public AirLiftModel(PrecipitationSettings settings, WindStep step)
            {
                this.step = step;
                anticipationSteps = Math.Max(1, (int)Math.Round(settings.AirAnticipationTiles / step.LengthInTiles));
                liftBeforeRidge = settings.AirLiftBeforeRidge;
                riseShare = step.ShareForStep(settings.AirRiseRate);
                sinkShare = step.ShareForStep(settings.AirSinkRate);
            }

            /// <summary>The air's lifted height above vertex (x, y), given its lifted height above the vertex upwind of it.</summary>
            public float LiftedHeightAt(FloatMap ground, int x, int y, float liftedHeightUpwind)
            {
                float here = ground[x, y];
                float target = here + liftBeforeRidge * (HighestGroundAhead(ground, x, y) - here);
                float share = target > liftedHeightUpwind ? riseShare : sinkShare;
                float liftedHeight = liftedHeightUpwind + share * (target - liftedHeightUpwind);
                return Math.Max(liftedHeight, here);
            }

            private float HighestGroundAhead(FloatMap ground, int x, int y)
            {
                float highest = ground[x, y];
                for (int i = 1; i <= anticipationSteps; i++)
                {
                    int aheadX = x + i * step.Dx;
                    int aheadY = y + i * step.Dy;
                    if (!ground.InBounds(aheadX, aheadY)) break;
                    highest = Math.Max(highest, ground[aheadX, aheadY]);
                }
                return highest;
            }
        }

        /// <summary>
        /// Carries the air (see AirColumn) across the ground along each wind line, one vertex at a time. It enters at
        /// the upwind edge at IncomingAirMoisture. At each vertex, in order: the air is lifted (see AirLiftModel), part
        /// of its cloud rains, and the ground gives back GroundRainRecycling of that rain.
        /// Apart from the incoming air, the water only leaves as rain the ground keeps or with the air, so none is lost. Returns the rain at every vertex per tile of travel (see WindStep) and the
        /// humidity of the air at ground level after it.
        /// </summary>
        private static (FloatMap rain, FloatMap surfaceHumidity) SweepWind(FloatMap ground, int maxHeight, FloatMap temperature, TemperatureSettings temperatureSettings, PrecipitationSettings settings)
        {
            var step = new WindStep(settings.WindFrom);
            float cloudRainShare = step.ShareForStep(settings.CloudRainRate);
            var airLift = new AirLiftModel(settings, step);
            var size = ground.VertexSize;
            var rain = new FloatMap(ground.Size);
            var surfaceHumidity = new FloatMap(ground.Size);
            // The air leaving each vertex of the row just finished and of the row in progress; an upwind neighbor is
            // always in one of the two, so the whole map never needs storing.
            var previousRow = new AirColumn[size.X];
            var currentRow = new AirColumn[size.X];

            // Visit vertices upwind first so each one's upwind neighbor has already been processed.
            for (int yi = 0; yi < size.Y; yi++)
            {
                int y = step.Dy >= 0 ? yi : size.Y - 1 - yi;
                for (int xi = 0; xi < size.X; xi++)
                {
                    int x = step.Dx >= 0 ? xi : size.X - 1 - xi;

                    int upwindX = x - step.Dx;
                    int upwindY = y - step.Dy;
                    bool hasUpwind = ground.InBounds(upwindX, upwindY);
                    var upwind = hasUpwind ? UpwindColumn(step, upwindX, previousRow, currentRow) : default;

                    float liftedHeight = airLift.LiftedHeightAt(ground, x, y, hasUpwind ? upwind.LiftedHeight : ground[x, y]);
                    float liftedAirTemperature = TemperatureGenerator.TemperatureAtHeight(liftedHeight, maxHeight, temperatureSettings);
                    float liftedCapacity = AirColumn.CapacityAt(liftedAirTemperature, settings.CapacityGrowth);
                    float groundCapacity = AirColumn.CapacityAt(temperature[x, y], settings.CapacityGrowth);

                    var column = hasUpwind
                        ? upwind.LiftedTo(liftedHeight)
                        : AirColumn.Entering(liftedHeight, liftedCapacity, settings.IncomingAirMoisture);
                    column = column
                        .Rained(liftedCapacity, settings.CloudFormationHumidity, cloudRainShare, out float fallen)
                        .Recycled(groundCapacity, settings.GroundRainRecycling * fallen);

                    rain[x, y] = step.AmountPerTile(fallen);
                    surfaceHumidity[x, y] = column.SurfaceHumidity(groundCapacity);
                    currentRow[x] = column;
                }
                (previousRow, currentRow) = (currentRow, previousRow);
            }
            return (rain, surfaceHumidity);
        }

        /// <summary>The air leaving the upwind neighbor: in the row in progress for an east or west wind, otherwise in the row just finished.</summary>
        private static AirColumn UpwindColumn(WindStep step, int upwindX, AirColumn[] previousRow, AirColumn[] currentRow) =>
            step.Dy == 0 ? currentRow[upwindX] : previousRow[upwindX];
    }
}
