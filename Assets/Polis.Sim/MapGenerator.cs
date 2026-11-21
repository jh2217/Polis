using System;

namespace Polis.Sim
{
    /// <summary>Runs the generators in order (elevation, then climate) and gathers their output into a MapState.</summary>
    public static class MapGenerator
    {
        [Serializable]
        public struct Settings
        {
            public ElevationGenerator.Settings Elevation;
            public ClimateGenerator.Settings Climate;

            public static Settings Default => new Settings
            {
                Elevation = ElevationGenerator.Settings.Default,
                Climate = ClimateGenerator.Settings.Default,
            };
        }

        /// <param name="size">Map size in tiles.</param>
        /// <param name="rng">Source of the map's seed when Elevation.UseSeed is false.</param>
        public static MapState Generate(MapSize size, SimRng rng, Settings settings)
        {
            var elevation = ElevationGenerator.Generate(size, rng, settings.Elevation);
            var climate = ClimateGenerator.Generate(elevation.Heights, settings.Elevation.MaxHeight, elevation.Seed, settings.Climate);
            return new MapState(elevation.Heights, elevation.Flatness, climate.Temperature, climate.Moisture);
        }
    }
}
