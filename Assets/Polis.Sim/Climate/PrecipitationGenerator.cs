using System;
using Polis.Sim.Grids;

namespace Polis.Sim.Climate
{
    /// <summary>
    /// Builds the precipitation field, a precipitation index in [0, 1] derived from it (see ClimateIndex), and the
    /// humidity of the air at ground level. Across a map a few km wide, rain comes from weather far larger than the
    /// map, so every vertex gets the region's AnnualPrecipitation. The region's air reads more humid over colder
    /// ground, which can hold less water.
    /// </summary>
    internal static class PrecipitationGenerator
    {
        public readonly struct Result
        {
            /// <summary>Precipitation at each vertex, in mm per year.</summary>
            public readonly FloatMap Precipitation;
            /// <summary>Precipitation mapped to [0, 1] (see ClimateIndex.PrecipitationIndexOf).</summary>
            public readonly FloatMap PrecipitationIndex;
            /// <summary>Relative humidity of the air at ground level at each vertex, in [0, 1]; 1 means fog.</summary>
            public readonly FloatMap SurfaceHumidity;

            public Result(FloatMap precipitation, FloatMap precipitationIndex, FloatMap surfaceHumidity)
            {
                Precipitation = precipitation;
                PrecipitationIndex = precipitationIndex;
                SurfaceHumidity = surfaceHumidity;
            }
        }

        /// <param name="temperature">Temperature at each vertex in degrees C, from TemperatureGenerator; the result has the same vertex layout.</param>
        public static Result Generate(FloatMap temperature, ClimateSettings climate)
        {
            climate = climate.Validated();
            var precipitation = new FloatMap(temperature.Size);
            var surfaceHumidity = new FloatMap(temperature.Size);
            for (int i = 0; i < precipitation.Data.Length; i++)
            {
                precipitation.Data[i] = climate.AnnualPrecipitation;
                surfaceHumidity.Data[i] = SurfaceHumidityAt(temperature.Data[i], climate);
            }
            return new Result(precipitation, ClimateIndex.PrecipitationIndexOf(precipitation), surfaceHumidity);
        }

        /// <summary>
        /// The relative humidity of the region's air over ground at the given temperature: the air holds the water of
        /// AirHumidity at sea-level temperature, and colder ground can hold less of it.
        /// </summary>
        private static float SurfaceHumidityAt(float groundTemperature, ClimateSettings climate)
        {
            // What air at sea-level temperature can hold, relative to what air at the ground's temperature can hold.
            float capacityRatio = (float)Math.Exp(Atmosphere.CapacityGrowthPerDegree * (climate.SeaLevelTemperature - groundTemperature));
            return Math.Min(1f, climate.AirHumidity * capacityRatio);
        }
    }
}
