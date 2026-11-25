using System;

namespace Polis.Sim
{
    /// <summary>Inputs to ElevationGenerator. Validated brings every value into its valid range.</summary>
    // System.Serializable (not a UnityEngine type) lets Unity show this struct in the inspector.
    [Serializable]
    public struct ElevationSettings
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
        /// <summary>Highest vertex height in height units; heights are whole numbers from 0 to this.</summary>
        public int MaxHeight;
        /// <summary>Flatness noise features per map width; low values make large plains and large rough regions.</summary>
        public float FlatnessScale;
        /// <summary>How much of the fine-detail octaves is removed where flatness is highest (0 disables plains, 1 is as smooth as the settings allow).</summary>
        public float FlatnessStrength;
        /// <summary>First octave (0-based) that flatness suppresses; earlier octaves keep the large-scale shape.</summary>
        public int FlatnessFromOctave;

        public static ElevationSettings Default => new ElevationSettings
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

        /// <summary>A copy with every value brought into its valid range; Generate does this itself.</summary>
        public ElevationSettings Validated()
        {
            var s = this;
            s.Scale = Math.Max(0f, s.Scale);
            s.Octaves = Math.Max(1, s.Octaves);
            s.MaxHeight = Math.Max(1, s.MaxHeight);
            s.FlatnessScale = Math.Max(0f, s.FlatnessScale);
            s.FlatnessStrength = Math.Clamp(s.FlatnessStrength, 0f, 1f);
            s.FlatnessFromOctave = Math.Max(0, s.FlatnessFromOctave);
            return s;
        }
    }
}
