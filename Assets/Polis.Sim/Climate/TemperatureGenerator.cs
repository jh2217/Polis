using Polis.Sim.Grids;

namespace Polis.Sim.Climate
{
    /// <summary>
    /// Builds a temperature field in degrees C, and a temperature index in [0, 1] derived from it (see ClimateIndex).
    /// Across a map a few km wide the region's temperature only changes with height: the air cools by
    /// Atmosphere.CoolingPerKilometer as the ground rises.
    /// </summary>
    internal static class TemperatureGenerator
    {
        public readonly struct Result
        {
            /// <summary>Temperature at each vertex, in degrees C.</summary>
            public readonly FloatMap Temperature;
            /// <summary>Temperature mapped to [0, 1] (see ClimateIndex.TemperatureIndexOf).</summary>
            public readonly FloatMap TemperatureIndex;

            public Result(FloatMap temperature, FloatMap temperatureIndex)
            {
                Temperature = temperature;
                TemperatureIndex = temperatureIndex;
            }
        }

        /// <param name="heights">Vertex heights; the result has the same vertex layout.</param>
        public static Result Generate(HeightMap heights, ClimateSettings climate)
        {
            climate = climate.Validated();
            var temperature = new FloatMap(heights.Size);
            for (int y = 0; y < temperature.VertexSize.Y; y++)
            for (int x = 0; x < temperature.VertexSize.X; x++)
                temperature[x, y] = TemperatureAtHeight(heights[x, y], climate);
            return new Result(temperature, ClimateIndex.TemperatureIndexOf(temperature));
        }

        /// <summary>The temperature in degrees C at the given height, in height units: sea level cooled by the lapse rate.</summary>
        public static float TemperatureAtHeight(float height, ClimateSettings climate)
        {
            climate = climate.Validated();
            float meters = height * CityScale.MetersPerHeightUnit;
            return climate.SeaLevelTemperature - Atmosphere.CoolingPerKilometer * meters / 1000f;
        }
    }
}
