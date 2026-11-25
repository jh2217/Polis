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
        /// <summary>The typical distance between hills, in metres; lower values make busier terrain. A larger map shows more hills, not bigger ones.</summary>
        public float HillSpacing;
        /// <summary>Number of noise layers summed; more adds finer detail.</summary>
        public int Octaves;
        /// <summary>Frequency multiplier between successive octaves.</summary>
        public float Lacunarity;
        /// <summary>Amplitude multiplier between successive octaves (lower = smoother terrain).</summary>
        public float Persistence;
        /// <summary>Power-curve exponent; above 1 pushes most land low (plains) and keeps peaks rare.</summary>
        public float Redistribution;
        /// <summary>The highest ground above the lowest, in metres; heights are whole height steps from 0 up to this.</summary>
        public float MaxElevation;
        /// <summary>The typical width of plains, and of the rough land between them, in metres.</summary>
        public float PlainSize;
        /// <summary>How much of the fine-detail octaves is removed where flatness is highest (0 disables plains, 1 is as smooth as the settings allow).</summary>
        public float FlatnessStrength;
        /// <summary>First octave (0-based) that flatness suppresses; earlier octaves keep the large-scale shape.</summary>
        public int FlatnessFromOctave;

        public static ElevationSettings Default => new ElevationSettings
        {
            UseSeed = false,
            Seed = 0,
            HillSpacing = 200f,
            Octaves = 5,
            Lacunarity = 2f,
            Persistence = 0.5f,
            Redistribution = 1.8f,
            MaxElevation = 160f,
            PlainSize = 600f,
            FlatnessStrength = 0.9f,
            FlatnessFromOctave = 2,
        };

        /// <summary>A copy with every value brought into its valid range; Generate does this itself.</summary>
        public ElevationSettings Validated()
        {
            var s = this;
            s.HillSpacing = Math.Max(WorldScale.MetersPerTile, s.HillSpacing);
            s.Octaves = Math.Max(1, s.Octaves);
            s.MaxElevation = Math.Max(WorldScale.MetersPerHeightUnit, s.MaxElevation);
            s.PlainSize = Math.Max(WorldScale.MetersPerTile, s.PlainSize);
            s.FlatnessStrength = Math.Clamp(s.FlatnessStrength, 0f, 1f);
            s.FlatnessFromOctave = Math.Max(0, s.FlatnessFromOctave);
            return s;
        }
    }
}
