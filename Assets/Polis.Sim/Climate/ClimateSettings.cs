using System;

namespace Polis.Sim.Climate
{
    /// <summary>The climate of the region the map lies in. Validated brings every value into its valid range.</summary>
    [Serializable]
    public struct ClimateSettings
    {
        /// <summary>The coldest SeaLevelTemperature allowed, in degrees C.</summary>
        public const float MinSeaLevelTemperature = -50f;
        /// <summary>The hottest SeaLevelTemperature allowed, in degrees C.</summary>
        public const float MaxSeaLevelTemperature = 50f;
        /// <summary>The region's mean temperature at sea level, in degrees C; higher ground is colder.</summary>
        public float SeaLevelTemperature;
        /// <summary>The region's rain and snow in mm per year, the same everywhere on the map.</summary>
        public float AnnualPrecipitation;
        /// <summary>The relative humidity of the region's air at sea-level temperature; colder ground reads more humid.</summary>
        public float AirHumidity;

        public static ClimateSettings Default => new ClimateSettings
        {
            SeaLevelTemperature = 15f,
            AnnualPrecipitation = 800f,
            AirHumidity = 0.7f,
        };

        /// <summary>A copy with every value brought into its valid range; the generators do this themselves.</summary>
        public ClimateSettings Validated()
        {
            var s = this;
            s.SeaLevelTemperature = Math.Clamp(s.SeaLevelTemperature, MinSeaLevelTemperature, MaxSeaLevelTemperature);
            s.AnnualPrecipitation = Math.Max(0f, s.AnnualPrecipitation);
            s.AirHumidity = Math.Clamp(s.AirHumidity, 0f, 1f);
            return s;
        }
    }
}
