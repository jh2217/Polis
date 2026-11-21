using System;

namespace Polis.Sim
{
    /// <summary>Builds a base heightmap of integer vertex heights from fBm noise plus a redistribution curve.</summary>
    public static class ElevationGenerator
    {
        // System.Serializable (not a UnityEngine type) lets Unity show this struct in the inspector.
        [Serializable]
        public struct Settings
        {
            /// <summary>When true, Seed pins the map; when false, the RNG passed to Generate decides.</summary>
            public bool UseSeed;
            /// <summary>Seed for deterministic generation of the same map; only used if UseSeed is true.</summary>
            public ulong Seed;
            /// <summary>Noise features per map width (higher = busier terrain).</summary>
            public float Scale;
            /// <summary>Number of noise layers summed; more adds finer detail.</summary>
            public int Octaves;
            /// <summary>Frequency multiplier between successive octaves.</summary>
            public float Lacunarity;
            /// <summary>Amplitude multiplier between successive octaves (lower = smoother terrain).</summary>
            public float Persistence;
            /// <summary>Power-curve exponent; above 1 pushes most land low (plains) and keeps peaks rare.</summary>
            public float Redistribution;
            /// <summary>Highest vertex height in height units (minimum 1); heights are whole numbers from 0 to this.</summary>
            public int MaxHeight;
            /// <summary>Flatness noise features per map width; low values make large plains and large rough regions.</summary>
            public float FlatnessScale;
            /// <summary>0 to 1: how much of the fine-detail octaves is removed where flatness is highest (0 disables plains, 1 is as smooth as the settings allow).</summary>
            public float FlatnessStrength;
            /// <summary>First octave (0-based) that flatness suppresses; earlier octaves keep the large-scale shape.</summary>
            public int FlatnessFromOctave;

            public static Settings Default => new Settings
            {
                UseSeed = false,
                Seed = 0,
                Scale = 6f,
                Octaves = 5,
                Lacunarity = 2f,
                Persistence = 0.5f,
                Redistribution = 1.8f,
                MaxHeight = 32,
                FlatnessScale = 2f,
                FlatnessStrength = 0.9f,
                FlatnessFromOctave = 2,
            };
        }

        // Keeps the flatness field independent of the elevation noise while sharing the same seed.
        private const ulong FlatnessSeedSalt = 0xA24BAED4963EE407UL;

        /// <param name="size">Map size in tiles; heights are generated for every vertex, so (X + 1) by (Y + 1) of them.</param>
        public static HeightMap Generate(MapSize size, SimRng rng, Settings settings)
        {
            ulong seed = settings.UseSeed ? settings.Seed : rng.NextULong();
            var elevationNoise = new Noise(seed);
            var flatnessNoise = new Noise(seed ^ FlatnessSeedSalt);
            var map = new HeightMap(size);
            float extent = Math.Max(size.X, size.Y);
            float elevationFrequency = settings.Scale / extent;
            float flatnessFrequency = settings.FlatnessScale / extent;
            float strength = Math.Min(1f, Math.Max(0f, settings.FlatnessStrength));
            int maxHeight = Math.Max(1, settings.MaxHeight);

            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
            {
                float flatness = flatnessNoise.Value(x * flatnessFrequency, y * flatnessFrequency);
                float detailScale = 1f - strength * flatness;
                float n = elevationNoise.Fbm(x * elevationFrequency, y * elevationFrequency, settings.Octaves,
                    settings.Lacunarity, settings.Persistence, settings.FlatnessFromOctave, detailScale);
                float smooth = (float)Math.Pow(n, settings.Redistribution);
                map[x, y] = (int)Math.Round(smooth * maxHeight);
            }
            return map;
        }
    }
}
