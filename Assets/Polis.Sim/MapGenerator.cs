using System;

namespace Polis.Sim
{
    /// <summary>Runs the generators in order (elevation, temperature, then precipitation) and gathers their output into a MapState.</summary>
    public static class MapGenerator
    {
        /// <param name="size">Map size in tiles.</param>
        /// <param name="rng">Source of the map's seed when Elevation.UseSeed is false.</param>
        public static MapState Generate(MapSize size, SimRng rng, MapSettings settings)
        {
            settings = settings.Validated();
            var elevation = ElevationGenerator.Generate(size, rng, settings.Elevation);
            var temperature = TemperatureGenerator.Generate(elevation.Heights, settings.Climate);
            var precipitation = PrecipitationGenerator.Generate(temperature.Temperature, settings.Climate);
            return new MapState(elevation.Heights, elevation.Flatness, temperature.Temperature, temperature.TemperatureIndex, precipitation.Precipitation, precipitation.PrecipitationIndex, precipitation.SurfaceHumidity);
        }
    }
}
