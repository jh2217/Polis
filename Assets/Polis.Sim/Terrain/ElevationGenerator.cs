using System;
using Polis.Sim.Grids;

namespace Polis.Sim.Terrain
{
    /// <summary>
    /// Builds a base heightmap of integer vertex heights from fBm noise plus a redistribution curve, with a
    /// low-frequency flatness field that removes fine detail to make plains.
    /// </summary>
    internal static class ElevationGenerator
    {
        // Keeps the flatness field independent of the elevation noise while sharing the same seed.
        private const ulong FlatnessSeedSalt = 0xA24BAED4963EE407UL;

        public readonly struct Result
        {
            public readonly HeightMap Heights;
            /// <summary>The flatness field in [0, 1] at every vertex, before FlatnessStrength is applied.</summary>
            public readonly FloatMap Flatness;
            /// <summary>The seed the map was generated from: ElevationSettings.Seed, or the one drawn from the RNG. Other generators can share it.</summary>
            public readonly ulong Seed;

            public Result(HeightMap heights, FloatMap flatness, ulong seed)
            {
                Heights = heights;
                Flatness = flatness;
                Seed = seed;
            }
        }

        /// <param name="size">Map size in cells; values are generated for every vertex, so (X + 1) by (Y + 1) of them.</param>
        /// <param name="scale">The real size of a cell and a height step, which turns the settings' metres into cells and steps.</param>
        public static Result Generate(MapSize size, SimRng rng, ElevationSettings settings, GridScale scale)
        {
            settings = settings.Validated();
            ulong seed = settings.UseSeed ? settings.Seed : rng.NextULong();
            var elevationNoise = new Noise(seed);
            var flatnessNoise = new Noise(seed ^ FlatnessSeedSalt);
            var map = new HeightMap(size);
            var flatnessMap = new FloatMap(size);
            // Noise cycles per vertex: vertices are a cell apart, so a feature of the given size spans size / MetersPerCell of them.
            float elevationFrequency = scale.MetersPerCell / settings.HillSpacing;
            float flatnessFrequency = scale.MetersPerCell / settings.PlainSize;
            int maxHeight = Math.Max(1, scale.HeightStepsIn(settings.MaxElevation));

            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
            {
                float flatness = flatnessNoise.Value(x * flatnessFrequency, y * flatnessFrequency);
                flatnessMap[x, y] = flatness;
                float detailScale = 1f - settings.FlatnessStrength * flatness;
                float n = elevationNoise.Fbm(x * elevationFrequency, y * elevationFrequency, settings.Octaves,
                    settings.Lacunarity, settings.Persistence, settings.FlatnessFromOctave, detailScale);
                float smooth = (float)Math.Pow(n, settings.Redistribution);
                map[x, y] = (int)Math.Round(smooth * maxHeight);
            }
            return new Result(map, flatnessMap, seed);
        }
    }
}
