namespace Polis.Sim
{
    /// <summary>Physical properties of air, the same everywhere.</summary>
    internal static class Atmosphere
    {
        /// <summary>How many degrees C colder the air is per kilometre of height (the standard lapse rate).</summary>
        public const float CoolingPerKilometer = 6.5f;

        /// <summary>How much more water air can hold per degree C warmer, as a fraction: about 6.5%.</summary>
        public const float CapacityGrowthPerDegree = 0.065f;
    }
}
