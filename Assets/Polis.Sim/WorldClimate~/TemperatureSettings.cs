using System;

namespace Polis.Sim
{
    /// <summary>Inputs to TemperatureGenerator. Validated brings every value into its valid range.</summary>
    [Serializable]
    public struct TemperatureSettings
    {
        /// <summary>The temperature in degrees C that gives a temperature index of 0 (and below it, still 0).</summary>
        public float ColdestTemperature;
        /// <summary>The smallest gap allowed between ColdestTemperature and HottestTemperature, in degrees C; the index divides by it.</summary>
        public const float MinTemperatureSpan = 1f;
        /// <summary>The temperature in degrees C that gives a temperature index of 1 (and above it, still 1).</summary>
        public float HottestTemperature;
        /// <summary>The coldest SeaLevelTemperature allowed, in degrees C.</summary>
        public const float MinSeaLevelTemperature = -50f;
        /// <summary>The hottest SeaLevelTemperature allowed, in degrees C.</summary>
        public const float MaxSeaLevelTemperature = 50f;
        /// <summary>The mean temperature at sea level, in degrees C.</summary>
        public float SeaLevelTemperature;
        /// <summary>Temperature noise features per map width.</summary>
        public float TemperatureScale;
        /// <summary>How far the noise moves the temperature between its coldest and warmest possible values, in degrees C. 0 turns the noise off.</summary>
        public float TemperatureVariation;
        /// <summary>How many degrees C colder the highest ground (MaxHeight) is than sea level, in a straight line between the two.</summary>
        public float ElevationCooling;

        public static TemperatureSettings Default => new TemperatureSettings
        {
            ColdestTemperature = -10f,
            HottestTemperature = 30f,
            SeaLevelTemperature = 15f,
            TemperatureScale = 1.5f,
            TemperatureVariation = 30f,
            ElevationCooling = 25f,
        };

        /// <summary>A copy with every value brought into its valid range; Generate does this itself.</summary>
        public TemperatureSettings Validated()
        {
            var s = this;
            s.SeaLevelTemperature = Math.Clamp(s.SeaLevelTemperature, MinSeaLevelTemperature, MaxSeaLevelTemperature);
            s.TemperatureScale = Math.Max(0f, s.TemperatureScale);
            s.TemperatureVariation = Math.Max(0f, s.TemperatureVariation);
            s.ElevationCooling = Math.Max(0f, s.ElevationCooling);
            s.HottestTemperature = Math.Max(s.HottestTemperature, s.ColdestTemperature + MinTemperatureSpan);
            return s;
        }
    }
}
