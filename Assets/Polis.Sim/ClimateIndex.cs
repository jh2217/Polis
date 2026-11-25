using System;

namespace Polis.Sim
{
    /// <summary>
    /// Maps temperature and precipitation to [0, 1] indices for a Whittaker-style biome lookup. The scales are the
    /// same for every map, so a hotter or wetter region reads hotter or wetter.
    /// </summary>
    internal static class ClimateIndex
    {
        /// <summary>The temperature in degrees C with a temperature index of 0 (and below it, still 0).</summary>
        public const float ColdestTemperature = -10f;

        /// <summary>The temperature in degrees C with a temperature index of 1 (and above it, still 1).</summary>
        public const float HottestTemperature = 30f;

        /// <summary>The precipitation in mm per year with a precipitation index of about 0.63.</summary>
        public const float PrecipitationReference = 1000f;

        /// <summary>Each temperature mapped linearly from ColdestTemperature (0) to HottestTemperature (1), held at the ends.</summary>
        public static FloatMap TemperatureIndexOf(FloatMap temperature)
        {
            var index = new FloatMap(temperature.Size);
            for (int i = 0; i < index.Data.Length; i++)
                index.Data[i] = Math.Clamp((temperature.Data[i] - ColdestTemperature) / (HottestTemperature - ColdestTemperature), 0f, 1f);
            return index;
        }

        /// <summary>
        /// Each precipitation mapped on a smooth curve, so heavy rain still reads wetter than moderate rain:
        /// 1 - e^(-precipitation / PrecipitationReference).
        /// </summary>
        public static FloatMap PrecipitationIndexOf(FloatMap precipitation)
        {
            var index = new FloatMap(precipitation.Size);
            for (int i = 0; i < index.Data.Length; i++)
                index.Data[i] = 1f - (float)Math.Exp(-Math.Max(0f, precipitation.Data[i]) / PrecipitationReference);
            return index;
        }
    }
}
