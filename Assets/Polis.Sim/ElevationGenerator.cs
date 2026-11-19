using System;

namespace Polis.Sim
{
    /// <summary>Builds a base heightmap in [0, 1] from fBm noise plus a redistribution curve.</summary>
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

            public static Settings Default => new Settings
            {
                UseSeed = false,
                Seed = 0,
                Scale = 3f,
                Octaves = 5,
                Lacunarity = 2f,
                Persistence = 0.5f,
                Redistribution = 1.8f,
            };
        }

        public static HeightMap Generate(MapSize size, SimRng rng, Settings settings)
        {
            var noise = new Noise(settings.UseSeed ? settings.Seed : rng.NextULong());
            var map = new HeightMap(size);
            float inv = settings.Scale / Math.Max(size.X, size.Y);

            for (int y = 0; y < size.Y; y++)
            for (int x = 0; x < size.X; x++)
            {
                float n = noise.Fbm(x * inv, y * inv, settings.Octaves, settings.Lacunarity, settings.Persistence);
                map[x, y] = (float)Math.Pow(n, settings.Redistribution);
            }
            return map;
        }
    }
}
