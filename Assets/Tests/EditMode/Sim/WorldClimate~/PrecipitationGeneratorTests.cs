using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class PrecipitationGeneratorTests
    {
        private static HeightMap SlopedMap(int size, int maxHeight)
        {
            var map = new HeightMap(size, size);
            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
                map[x, y] = maxHeight * x / size;
            return map;
        }

        // A 64 x 16 map, flat except for a north-south ridge: rising 2 units per tile from x = 24 to a
        // crest of 16 at x = 32, then falling back to 0 at x = 40.
        private static HeightMap RidgeMap()
        {
            var map = new HeightMap(64, 16);
            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
                map[x, y] = System.Math.Max(0, 16 - 2 * System.Math.Abs(x - 32));
            return map;
        }

        // A 64 x 16 map that is flat at 0 up to x = 9 and flat at 16 from x = 10 on.
        private static HeightMap StepMap()
        {
            var map = new HeightMap(64, 16);
            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 10; x < map.VertexSize.X; x++)
                map[x, y] = 16;
            return map;
        }

        private static HeightMap FlatMap() => new HeightMap(64, 16);

        // A temperature field of the map's size at one temperature everywhere, for ground warmer or colder than the air.
        private static FloatMap UniformTemperature(HeightMap map, float temperature)
        {
            var field = new FloatMap(map.Size);
            for (int i = 0; i < field.Data.Length; i++) field.Data[i] = temperature;
            return field;
        }

        // Temperature settings with no noise, so the results do not depend on the seed: the temperature is the same
        // everywhere at the same height.
        private static TemperatureSettings NoNoise()
        {
            var s = TemperatureSettings.Default;
            s.TemperatureVariation = 0f;
            return s;
        }

        // Precipitation settings with nothing blurred, the air following the ground exactly and no rain given back
        // by the ground, so the rain changes right where the ground does and only comes from the incoming air, which
        // is humid enough to rain on flat land.
        private static PrecipitationSettings Calm()
        {
            var s = PrecipitationSettings.Default;
            s.IncomingAirMoisture = 0.9f;
            s.GroundRainRecycling = 0f;
            s.RainShadowSmoothing = 0;
            s.AirLiftBeforeRidge = 0f;
            s.AirRiseRate = 1f;
            s.AirSinkRate = 1f;
            return s;
        }

        private static PrecipitationSettings Calm(CompassDirection windFrom)
        {
            var s = Calm();
            s.WindFrom = windFrom;
            return s;
        }

        private static PrecipitationGenerator.Result Run(HeightMap map, PrecipitationSettings settings, int maxHeight = 16, TemperatureSettings? temperatureSettings = null)
        {
            var t = temperatureSettings ?? NoNoise();
            var temperature = TemperatureGenerator.Generate(map, maxHeight, 7, t).Temperature;
            return PrecipitationGenerator.Generate(map, maxHeight, temperature, t, settings);
        }

        private static float MeanOver(FloatMap field, int fromX, int toX)
        {
            float total = 0f;
            int count = 0;
            for (int y = 0; y < field.VertexSize.Y; y++)
            for (int x = fromX; x <= toX; x++)
            {
                total += field[x, y];
                count++;
            }
            return total / count;
        }

        // The vertex where a lane starts for the given wind on a 64 x 16 map: its upwind neighbor is off the map.
        private static (int x, int y) LaneStart(CompassDirection windFrom)
        {
            var (dx, dy) = windFrom.Opposite().Step();
            return (dx > 0 ? 0 : dx < 0 ? 64 : 32, dy > 0 ? 0 : dy < 0 ? 16 : 8);
        }

        [Test]
        public void Generate_HasOneFiniteValuePerVertex_AndTheIndexIsInUnitRange()
        {
            var result = Run(SlopedMap(24, 10), PrecipitationSettings.Default, 10, TemperatureSettings.Default);

            Assert.AreEqual(25 * 25, result.Precipitation.Data.Length);
            Assert.AreEqual(25 * 25, result.PrecipitationIndex.Data.Length);
            foreach (float v in result.Precipitation.Data) Assert.IsTrue(v >= 0f && !float.IsInfinity(v));
            foreach (float v in result.PrecipitationIndex.Data) Assert.That(v, Is.InRange(0f, 1f));
        }

        [Test]
        public void Generate_CloudFormationHumidityOfOne_KeepsEveryValueFinite_AndStillRainsWhereAirIsOversaturated()
        {
            var s = Calm();
            s.CloudFormationHumidity = 1f;
            s.IncomingAirMoisture = 1f;

            var result = Run(SlopedMap(24, 10), s, 10, TemperatureSettings.Default);

            foreach (float v in result.Precipitation.Data) Assert.IsTrue(v >= 0f && !float.IsInfinity(v));
            foreach (float v in result.PrecipitationIndex.Data) Assert.That(v, Is.InRange(0f, 1f));
            Assert.Greater(MeanOver(result.Precipitation, 0, 24), 0f);
        }

        [Test]
        public void Generate_SameInputs_GiveTheSameResult()
        {
            var a = Run(SlopedMap(24, 10), PrecipitationSettings.Default, 10, TemperatureSettings.Default);
            var b = Run(SlopedMap(24, 10), PrecipitationSettings.Default, 10, TemperatureSettings.Default);

            CollectionAssert.AreEqual(a.Precipitation.Data, b.Precipitation.Data);
            CollectionAssert.AreEqual(a.PrecipitationIndex.Data, b.PrecipitationIndex.Data);
        }

        [Test]
        public void Generate_DryAir_GivesNoRainOnFlatLand()
        {
            var s = Calm();
            s.IncomingAirMoisture = s.CloudFormationHumidity - 0.1f;

            var precipitation = Run(FlatMap(), s).Precipitation;

            foreach (float v in precipitation.Data) Assert.AreEqual(0f, v);
        }

        [Test]
        public void Generate_AirAboveTheCloudFormationHumidity_RainsOnFlatLandUntilItDriesToIt()
        {
            var s = Calm();
            s.IncomingAirMoisture = 1f;

            var precipitation = Run(FlatMap(), s).Precipitation;

            Assert.Greater(MeanOver(precipitation, 0, 10), 0f);
            Assert.Greater(MeanOver(precipitation, 0, 10), MeanOver(precipitation, 54, 64) * 5f);
        }

        [Test]
        public void Generate_AirEnteringAboveTheCloudFormationHumidity_RainsFromTheUpwindEdgeAndDecaysInland()
        {
            var s = Calm();
            s.IncomingAirMoisture = 0.9f;

            var precipitation = Run(FlatMap(), s).Precipitation;

            Assert.Greater(precipitation[0, 5], 0f);
            for (int x = 0; x < 20; x++)
                Assert.GreaterOrEqual(precipitation[x, 5], precipitation[x + 1, 5]);
        }

        [Test]
        public void Generate_SurfaceHumidity_StaysBetweenZeroAndOne_ForEveryWind()
        {
            foreach (CompassDirection wind in System.Enum.GetValues(typeof(CompassDirection)))
            {
                var s = PrecipitationSettings.Default;
                s.WindFrom = wind;
                s.IncomingAirMoisture = 1f;

                var surfaceHumidity = Run(RidgeMap(), s, 16, TemperatureSettings.Default).SurfaceHumidity;

                foreach (float v in surfaceHumidity.Data) Assert.That(v, Is.InRange(0f, 1f), wind.ToString());
            }
        }

        [Test]
        public void Generate_SurfaceHumidity_IsZeroForBoneDryAirThatGainsNoMoisture()
        {
            var s = Calm();
            s.IncomingAirMoisture = 0f;

            var surfaceHumidity = Run(RidgeMap(), s).SurfaceHumidity;

            foreach (float v in surfaceHumidity.Data) Assert.AreEqual(0f, v);
        }

        [Test]
        public void Generate_SurfaceHumidity_IsHigherOverColdGroundThanWarmGround_UnderTheSameAir()
        {
            var s = Calm();
            s.IncomingAirMoisture = 0.5f;
            var map = FlatMap();
            var temperatureSettings = NoNoise();

            var overCold = PrecipitationGenerator.Generate(map, 16, UniformTemperature(map, 5f), temperatureSettings, s).SurfaceHumidity;
            var overWarm = PrecipitationGenerator.Generate(map, 16, UniformTemperature(map, 25f), temperatureSettings, s).SurfaceHumidity;

            for (int i = 0; i < overCold.Data.Length; i++) Assert.Greater(overCold.Data[i], overWarm.Data[i]);
        }

        [Test]
        public void Generate_SurfaceHumidity_IsOneWhereTheGroundIsColdEnoughForFog()
        {
            // The air is too dry to cloud up (and rain) at its own temperature, but ground 25 degrees C colder
            // can hold far less than the air carries.
            var s = Calm();
            s.IncomingAirMoisture = 0.75f;
            var map = FlatMap();
            var temperatureSettings = NoNoise();

            var surfaceHumidity = PrecipitationGenerator.Generate(map, 16, UniformTemperature(map, temperatureSettings.SeaLevelTemperature - 25f), temperatureSettings, s).SurfaceHumidity;

            foreach (float v in surfaceHumidity.Data) Assert.AreEqual(1f, v);
        }

        [Test]
        public void Generate_WarmerMap_RainsMoreAtTheSameHumidity()
        {
            // Warm air holds more water, so air entering at the same humidity carries more of it and rains more.
            var s = Calm();
            s.IncomingAirMoisture = 0.9f;
            var cool = NoNoise();
            cool.SeaLevelTemperature = 5f;
            var warm = NoNoise();
            warm.SeaLevelTemperature = 25f;

            var coolRain = Run(RidgeMap(), s, 16, cool).Precipitation;
            var warmRain = Run(RidgeMap(), s, 16, warm).Precipitation;

            Assert.Greater(MeanOver(warmRain, 0, 64), MeanOver(coolRain, 0, 64) * 2f);
        }

        [Test]
        public void Generate_DryAirOverDryPlains_StaysDry_WhateverTheRecycling()
        {
            var s = Calm();
            s.IncomingAirMoisture = 0.5f; // below the cloud formation humidity, so nothing rains
            s.GroundRainRecycling = 1f;

            var result = Run(FlatMap(), s);

            foreach (float v in result.Precipitation.Data) Assert.AreEqual(0f, v);
            foreach (float v in result.SurfaceHumidity.Data) Assert.AreEqual(0.5f, v, 1e-5f);
        }

        [Test]
        public void Generate_RainRecycling_CarriesRainFurtherInland()
        {
            var soaked = Calm();
            soaked.IncomingAirMoisture = 1f;
            var recycled = soaked;
            recycled.GroundRainRecycling = 0.5f;

            var soakedRain = Run(FlatMap(), soaked).Precipitation;
            var recycledRain = Run(FlatMap(), recycled).Precipitation;

            Assert.Greater(MeanOver(recycledRain, 40, 64), MeanOver(soakedRain, 40, 64));
        }

        [Test]
        public void Generate_RainRecycling_NeverRainsMoreThanTheAirBrought()
        {
            // Each lane's air brings 1 unit (saturated at 15 degrees C on flat noiseless ground). The ground gives back
            // half of what falls, so the water the lane loses to the ground is half its rain, and that is at most 1.
            var s = Calm();
            s.IncomingAirMoisture = 1f;
            s.GroundRainRecycling = 0.5f;

            var precipitation = Run(FlatMap(), s).Precipitation;

            for (int y = 0; y < precipitation.VertexSize.Y; y++)
            {
                float rained = 0f;
                for (int x = 0; x < precipitation.VertexSize.X; x++) rained += precipitation[x, y];
                Assert.LessOrEqual(rained * 0.5f, 1f + 1e-4f, $"lane {y}");
            }
        }

        [Test]
        public void Generate_RainShadow_DriesTheLandDownwindOfARidge()
        {
            var s = Calm();
            var flat = Run(FlatMap(), s).Precipitation;
            var ridge = Run(RidgeMap(), s).Precipitation;

            float flatMean = MeanOver(flat, 45, 56);
            Assert.Greater(flatMean, 0f); // otherwise there is nothing for the ridge to dry out
            Assert.Less(MeanOver(ridge, 45, 56), flatMean * 0.5f);
        }

        [Test]
        public void Generate_RainShadow_RainsOnTheSlopeFacingTheWind()
        {
            var s = Calm();
            var flat = Run(FlatMap(), s).Precipitation;
            var ridge = Run(RidgeMap(), s).Precipitation;

            // The air cools as the ground rises from x = 24, so it condenses and rains.
            Assert.Greater(MeanOver(ridge, 25, 31), MeanOver(flat, 25, 31));
        }

        [Test]
        public void Generate_RainShadow_LeavesLandWellUpwindOfTheRidgeUnchanged()
        {
            var s = Calm();
            var flat = Run(FlatMap(), s).Precipitation;
            var ridge = Run(RidgeMap(), s).Precipitation;

            // The ridge starts at x = 24.
            for (int y = 0; y < flat.VertexSize.Y; y++)
            for (int x = 0; x <= 23; x++)
                Assert.AreEqual(flat[x, y], ridge[x, y], 1e-6f);
        }

        [Test]
        public void Generate_AirLiftBeforeRidge_RainsOnTheFoothillsBeforeTheRidgeStartsToRise()
        {
            var plain = Calm();
            var lifted = plain;
            lifted.AirLiftBeforeRidge = 1f;
            lifted.AirAnticipationTiles = 6f;

            var plainRain = Run(RidgeMap(), plain).Precipitation;
            var liftedRain = Run(RidgeMap(), lifted).Precipitation;

            // The ground is flat up to x = 24, so rain just before it can only come from the air lifting early.
            Assert.Greater(MeanOver(liftedRain, 19, 23), MeanOver(plainRain, 19, 23));
        }

        [Test]
        public void Generate_AirSinkRate_KeepsTheAirColdPastTheCrestSoItKeepsRaining()
        {
            var follows = Calm();
            var lingers = follows;
            lingers.AirSinkRate = 0.1f;

            var followsRain = Run(RidgeMap(), follows).Precipitation;
            var lingersRain = Run(RidgeMap(), lingers).Precipitation;

            // Air that stays high is still cold just past the crest, so its cloud does not evaporate and keeps raining.
            Assert.Greater(MeanOver(lingersRain, 33, 38), MeanOver(followsRain, 33, 38));
        }

        [Test]
        public void Generate_AirLift_NeverPutsTheAirBelowTheGround_SoFlatLandIsUnaffected()
        {
            var calm = Calm();
            var inert = calm;
            inert.AirAnticipationTiles = PrecipitationSettings.Default.AirAnticipationTiles;
            inert.AirLiftBeforeRidge = PrecipitationSettings.Default.AirLiftBeforeRidge;
            inert.AirRiseRate = PrecipitationSettings.Default.AirRiseRate;
            inert.AirSinkRate = PrecipitationSettings.Default.AirSinkRate;

            var calmRain = Run(FlatMap(), calm).Precipitation;
            var inertRain = Run(FlatMap(), inert).Precipitation;

            CollectionAssert.AreEqual(calmRain.Data, inertRain.Data);
        }

        [Test]
        public void Generate_AirLift_ForEveryWind_KeepsTheFieldsFinite()
        {
            foreach (CompassDirection wind in System.Enum.GetValues(typeof(CompassDirection)))
            {
                var s = PrecipitationSettings.Default;
                s.WindFrom = wind;

                var result = Run(RidgeMap(), s);

                foreach (float v in result.Precipitation.Data) Assert.IsTrue(v >= 0f && !float.IsInfinity(v));
            }
        }

        [Test]
        public void Generate_CapacityGrowth_MakesAClimbRainMore()
        {
            var weak = Calm();
            weak.CapacityGrowth = 0.01f;
            var strong = weak;
            strong.CapacityGrowth = 0.06f;

            var weakRain = Run(RidgeMap(), weak).Precipitation;
            var strongRain = Run(RidgeMap(), strong).Precipitation;

            Assert.Greater(MeanOver(strongRain, 24, 32), MeanOver(weakRain, 24, 32));
        }

        [Test]
        public void Generate_RainShadowSmoothing_EvensOutNeighboringVerticesAcrossTheWind()
        {
            var rng = new SimRng(3);
            var bumpy = new HeightMap(64, 64);
            for (int i = 0; i < bumpy.Data.Length; i++)
                bumpy.Data[i] = rng.NextInt(8);
            var smooth = Calm();
            smooth.RainShadowSmoothing = 3;
            var streaky = Calm();

            float smoothSteps = MeanStepAcrossWind(Run(bumpy, smooth, 8).PrecipitationIndex);
            float streakySteps = MeanStepAcrossWind(Run(bumpy, streaky, 8).PrecipitationIndex);

            Assert.Less(smoothSteps, streakySteps * 0.75f);
        }

        // Mean difference between vertically adjacent vertices; the default wind comes from the west and blows east, so these lie on different wind lines.
        private static float MeanStepAcrossWind(FloatMap field)
        {
            float total = 0f;
            int count = 0;
            for (int y = 0; y + 1 < field.VertexSize.Y; y++)
            for (int x = 0; x < field.VertexSize.X; x++)
            {
                total += System.Math.Abs(field[x, y + 1] - field[x, y]);
                count++;
            }
            return total / count;
        }

        [Test]
        public void Generate_RainShadow_FollowsTheDirectionTheWindComesFrom()
        {
            var westerly = Calm(CompassDirection.West);
            var easterly = Calm(CompassDirection.East);

            var westerlyFlat = Run(FlatMap(), westerly).Precipitation;
            var westerlyRidge = Run(RidgeMap(), westerly).Precipitation;
            var easterlyFlat = Run(FlatMap(), easterly).Precipitation;
            var easterlyRidge = Run(RidgeMap(), easterly).Precipitation;

            // The land west of the ridge is upwind of it for a wind from the west, and in its shadow for one from the east.
            Assert.AreEqual(MeanOver(westerlyFlat, 4, 16), MeanOver(westerlyRidge, 4, 16), 1e-6f);
            Assert.Greater(MeanOver(easterlyFlat, 4, 20), 0f);
            Assert.Less(MeanOver(easterlyRidge, 4, 20), MeanOver(easterlyFlat, 4, 20) * 0.5f);
        }

        [Test]
        public void Generate_EveryWind_KeepsTheFieldsFiniteAndTheIndexInUnitRange()
        {
            foreach (CompassDirection wind in System.Enum.GetValues(typeof(CompassDirection)))
            {
                var s = PrecipitationSettings.Default;
                s.WindFrom = wind;

                var result = Run(RidgeMap(), s, 16, TemperatureSettings.Default);

                foreach (float v in result.Precipitation.Data) Assert.IsTrue(v >= 0f && !float.IsInfinity(v), wind.ToString());
                foreach (float v in result.PrecipitationIndex.Data) Assert.That(v, Is.InRange(0f, 1f), wind.ToString());
            }
        }

        [Test]
        public void Generate_RainOnALaneAddsUpToTheMoistureThatEnteredIt_ForAxisAndDiagonalWinds()
        {
            // The air enters below the cloud formation humidity, so the flat ground below the step gives no rain. At the step the ground
            // rises so far that the air's capacity collapses and nearly all its moisture condenses and, with every
            // bit of cloud falling at once, rains out.
            foreach (var wind in new[] { CompassDirection.West, CompassDirection.NorthWest })
            {
                var s = Calm(wind);
                s.IncomingAirMoisture = 0.7f;
                s.CloudRainRate = 1f;
                var temperatureSettings = NoNoise();
                temperatureSettings.ElevationCooling = 100f;

                var precipitation = Run(StepMap(), s, 16, temperatureSettings).Precipitation;

                var (x, y) = LaneStart(wind);
                var (dx, dy) = wind.Opposite().Step();
                float moistureRained = 0f;
                for (; precipitation.InBounds(x, y); x += dx, y += dy)
                    moistureRained += precipitation[x, y] * wind.StepLength();

                // The rain was spread over the tiles each step crossed, so scaling it back by the step length gives the moisture that fell.
                Assert.AreEqual(0.7f, moistureRained, 0.01f, wind.ToString());
            }
        }

        [Test]
        public void Generate_FlatLand_RainsTheSameAtTheSameDistanceFromTheUpwindEdge_ForEveryWind()
        {
            PrecipitationSettings Slow(CompassDirection wind)
            {
                var s = Calm(wind);
                s.IncomingAirMoisture = 1f;
                s.CloudRainRate = 0.05f; // small shares, so a step's length barely changes the order things happen in
                return s;
            }

            var west = Run(FlatMap(), Slow(CompassDirection.West)).Precipitation;

            foreach (CompassDirection wind in System.Enum.GetValues(typeof(CompassDirection)))
            {
                var precipitation = Run(FlatMap(), Slow(wind)).Precipitation;
                var (x0, y0) = LaneStart(wind);
                var (dx, dy) = wind.Opposite().Step();
                const int steps = 5;
                float distance = steps * wind.StepLength();
                int whole = (int)System.Math.Floor(distance + 1e-4f);
                float fraction = distance - whole;

                float rain = precipitation[x0 + steps * dx, y0 + steps * dy];

                float expected = west[whole, 5] * (1f - fraction) + west[whole + 1, 5] * fraction;
                Assert.AreEqual(expected, rain, expected * 0.08f, wind.ToString());
            }
        }

        [Test]
        public void Generate_PrecipitationIndex_IsZeroWhereNothingRains()
        {
            var s = Calm();
            s.IncomingAirMoisture = 0.5f;

            var index = Run(FlatMap(), s).PrecipitationIndex;

            foreach (float v in index.Data) Assert.AreEqual(0f, v);
        }

        [Test]
        public void Generate_PrecipitationIndex_IsAboutSixtyThreePercentAtThePrecipitationReference()
        {
            var s = Calm();
            s.IncomingAirMoisture = 1f;
            float rain = Run(FlatMap(), s).Precipitation[0, 5];
            Assert.Greater(rain, 0f);

            s.PrecipitationReference = rain;
            var index = Run(FlatMap(), s).PrecipitationIndex;

            Assert.AreEqual(0.632f, index[0, 5], 1e-3f);
        }

        [Test]
        public void Generate_PrecipitationIndex_RisesWithPrecipitationAndStaysBelowOneWhereRainIsModerate()
        {
            var s = Calm();
            s.IncomingAirMoisture = 1f;

            var result = Run(FlatMap(), s);

            for (int x = 1; x < result.Precipitation.VertexSize.X; x++)
            {
                float change = result.Precipitation[x, 5] - result.Precipitation[x - 1, 5];
                float indexChange = result.PrecipitationIndex[x, 5] - result.PrecipitationIndex[x - 1, 5];
                if (change > 1e-7f) Assert.Greater(indexChange, 0f);
                if (change < -1e-7f) Assert.Less(indexChange, 0f);
            }
            foreach (float v in result.PrecipitationIndex.Data) Assert.Less(v, 1f);
        }

        [Test]
        public void Generate_PrecipitationIndex_UsesOneScaleForEveryMap()
        {
            var wet = Calm();
            wet.IncomingAirMoisture = 1f;
            var drier = wet;
            drier.IncomingAirMoisture = 0.9f;

            var wetIndex = Run(FlatMap(), wet).PrecipitationIndex;
            var drierIndex = Run(FlatMap(), drier).PrecipitationIndex;

            // Scaling each map by its own maximum would make the two read the same.
            Assert.Greater(MeanOver(wetIndex, 0, 10), MeanOver(drierIndex, 0, 10) + 0.02f);
        }

        [Test]
        public void Generate_SaturatedAir_RainsMoreOnASlopeThanOnFlatLand()
        {
            var s = Calm();
            s.IncomingAirMoisture = 1f;
            var flat = Run(FlatMap(), s).PrecipitationIndex;
            var ridge = Run(RidgeMap(), s).PrecipitationIndex;

            Assert.Greater(MeanOver(ridge, 25, 31), MeanOver(flat, 25, 31) + 0.01f);
        }

        [Test]
        public void Generate_TemperatureSettings_SetHowMuchTheAirCoolsAsTheGroundRises()
        {
            var s = Calm();
            var mild = NoNoise();
            mild.ElevationCooling = 5f;
            var steep = mild;
            steep.ElevationCooling = 25f;

            var mildRain = Run(RidgeMap(), s, 16, mild).Precipitation;
            var steepRain = Run(RidgeMap(), s, 16, steep).Precipitation;

            Assert.Greater(MeanOver(steepRain, 24, 32), MeanOver(mildRain, 24, 32));
        }

        [Test]
        public void Generate_OutOfRangeSettings_GiveTheSameMapAsTheirValidatedForm()
        {
            var wild = PrecipitationSettings.Default;
            wild.IncomingAirMoisture = 5f;
            wild.GroundRainRecycling = 3f;
            wild.CloudFormationHumidity = 4f;

            var a = Run(RidgeMap(), wild);
            var b = Run(RidgeMap(), wild.Validated());

            CollectionAssert.AreEqual(a.Precipitation.Data, b.Precipitation.Data);
            CollectionAssert.AreEqual(a.PrecipitationIndex.Data, b.PrecipitationIndex.Data);
        }
    }
}
