using System;

namespace Polis.Sim
{
    /// <summary>
    /// Builds a temperature field in degrees C, and a temperature index in [0, 1] derived from it on a fixed scale
    /// (an input to a Whittaker-style biome lookup). Temperature is low-frequency noise around a sea-level mean,
    /// cooled with elevation.
    /// </summary>
    internal static class TemperatureGenerator
    {
        public readonly struct Result
        {
            /// <summary>Temperature at each vertex, in degrees C.</summary>
            public readonly FloatMap Temperature;
            /// <summary>Temperature mapped to [0, 1] by a fixed scale (See <see cref="TemperatureIndexOf"/>).</summary>
            public readonly FloatMap TemperatureIndex;

            public Result(FloatMap temperature, FloatMap temperatureIndex)
            {
                Temperature = temperature;
                TemperatureIndex = temperatureIndex;
            }
        }

        private const int Octaves = 3;

        // Keeps the temperature noise independent of the elevation noise while sharing one seed.
        private const ulong TemperatureSeedSalt = 0xD1B54A32D192ED03UL;

        /// <param name="heights">Vertex heights; the result has the same vertex layout.</param>
        /// <param name="maxHeight">The height that counts as full elevation for cooling (minimum 1).</param>
        /// <param name="seed">Seed shared with the rest of the map's generation.</param>
        public static Result Generate(HeightMap heights, int maxHeight, ulong seed, TemperatureSettings settings)
        {
            settings = settings.Validated();
            var temperature = GenerateTemperature(heights, maxHeight, seed ^ TemperatureSeedSalt, settings);
            return new Result(temperature, TemperatureIndexOf(temperature, settings));
        }

        /// <summary>
        /// The temperature in degrees C at the given height without the noise: sea level cooled in a straight line
        /// up to maxHeight, and no further above it.
        /// </summary>
        public static float TemperatureAtHeight(float height, int maxHeight, TemperatureSettings settings)
        {
            settings = settings.Validated();
            return settings.SeaLevelTemperature - settings.ElevationCooling * Math.Min(1f, height / Math.Max(1, maxHeight));
        }

        /// <summary>Low-frequency noise around the noiseless temperature at each vertex's height.</summary>
        private static FloatMap GenerateTemperature(HeightMap heights, int maxHeight, ulong seed, TemperatureSettings settings)
        {
            var noise = new Noise(seed);
            var temperature = new FloatMap(heights.Size);
            float frequency = settings.TemperatureScale / Math.Max(heights.Size.X, heights.Size.Y);

            for (int y = 0; y < temperature.VertexSize.Y; y++)
            for (int x = 0; x < temperature.VertexSize.X; x++)
            {
                float warmth = noise.Fbm(x * frequency, y * frequency, Octaves);
                temperature[x, y] = TemperatureAtHeight(heights[x, y], maxHeight, settings)
                    + settings.TemperatureVariation * (warmth - 0.5f);
            }
            return temperature;
        }

        /// <summary>
        /// Maps temperature to [0, 1] on a scale that is the same for every map: ColdestTemperature is 0 and
        /// HottestTemperature is 1, with colder and hotter values held at the ends.
        /// </summary>
        private static FloatMap TemperatureIndexOf(FloatMap temperature, TemperatureSettings settings)
        {
            float span = settings.HottestTemperature - settings.ColdestTemperature;
            var temperatureIndex = new FloatMap(temperature.Size);
            for (int i = 0; i < temperatureIndex.Data.Length; i++)
                temperatureIndex.Data[i] = Math.Clamp((temperature.Data[i] - settings.ColdestTemperature) / span, 0f, 1f);
            return temperatureIndex;
        }
    }
}
