using System;

namespace Polis.Sim
{
    /// <summary>
    /// Builds independent temperature and moisture fields in [0, 1] (the inputs to a Whittaker-style biome lookup).
    /// Temperature is low-frequency noise cooled with elevation; moisture is low-frequency noise.
    /// </summary>
    public static class ClimateGenerator
    {
        [Serializable]
        public struct Settings
        {
            /// <summary>Temperature noise features per map width.</summary>
            public float TemperatureScale;
            /// <summary>Moisture noise features per map width.</summary>
            public float MoistureScale;
            /// <summary>How much temperature drops from sea level to MaxHeight, as a fraction of the full 0 to 1 range.</summary>
            public float ElevationCooling;

            public static Settings Default => new Settings
            {
                TemperatureScale = 1.5f,
                MoistureScale = 2.5f,
                ElevationCooling = 0.5f,
            };
        }

        public readonly struct Result
        {
            public readonly FloatMap Temperature;
            public readonly FloatMap Moisture;

            public Result(FloatMap temperature, FloatMap moisture)
            {
                Temperature = temperature;
                Moisture = moisture;
            }
        }

        private const int Octaves = 3;

        // Keep the two fields independent of each other and of the elevation noise while sharing one seed.
        private const ulong TemperatureSeedSalt = 0xD1B54A32D192ED03UL;
        private const ulong MoistureSeedSalt = 0x8CB92BA72F3D8DD7UL;

        /// <param name="heights">Vertex heights; the result has the same vertex layout.</param>
        /// <param name="maxHeight">The height that counts as full elevation for cooling (minimum 1).</param>
        /// <param name="seed">Seed shared with the rest of the map's generation.</param>
        public static Result Generate(HeightMap heights, int maxHeight, ulong seed, Settings settings) =>
            new Result(
                GenerateTemperature(heights, maxHeight, seed ^ TemperatureSeedSalt, settings),
                GenerateMoisture(heights.Size, seed ^ MoistureSeedSalt, settings));

        /// <summary>Low-frequency noise, cooled in proportion to how high each vertex is.</summary>
        private static FloatMap GenerateTemperature(HeightMap heights, int maxHeight, ulong seed, Settings settings)
        {
            var noise = new Noise(seed);
            var temperature = new FloatMap(heights.Size);
            float frequency = settings.TemperatureScale / Math.Max(heights.Size.X, heights.Size.Y);
            float perHeight = 1f / Math.Max(1, maxHeight);

            for (int y = 0; y < temperature.VertexSize.Y; y++)
            for (int x = 0; x < temperature.VertexSize.X; x++)
            {
                float warmth = noise.Fbm(x * frequency, y * frequency, Octaves);
                float elevation = Math.Min(1f, heights[x, y] * perHeight);
                temperature[x, y] = Math.Max(0f, warmth - settings.ElevationCooling * elevation);
            }
            return temperature;
        }

        /// <summary>Low-frequency noise.</summary>
        private static FloatMap GenerateMoisture(MapSize size, ulong seed, Settings settings)
        {
            var noise = new Noise(seed);
            var moisture = new FloatMap(size);
            float frequency = settings.MoistureScale / Math.Max(size.X, size.Y);

            for (int y = 0; y < moisture.VertexSize.Y; y++)
            for (int x = 0; x < moisture.VertexSize.X; x++)
                moisture[x, y] = noise.Fbm(x * frequency, y * frequency, Octaves);
            return moisture;
        }
    }
}
