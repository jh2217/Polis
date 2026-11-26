using System;
using Polis.Sim.Climate;
using Polis.Sim.Terrain;

namespace Polis.Sim
{
    /// <summary>Inputs to MapGenerator: the terrain and the region's climate.</summary>
    [Serializable]
    public struct MapSettings
    {
        public ElevationSettings Elevation;
        public ClimateSettings Climate;

        public static MapSettings Default => new MapSettings
        {
            Elevation = ElevationSettings.Default,
            Climate = ClimateSettings.Default,
        };

        /// <summary>A copy with every part's settings brought into their valid ranges; Generate does this itself.</summary>
        public MapSettings Validated()
        {
            var s = this;
            s.Elevation = s.Elevation.Validated();
            s.Climate = s.Climate.Validated();
            return s;
        }
    }
}
