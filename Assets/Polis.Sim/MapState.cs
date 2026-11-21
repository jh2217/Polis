using System;

namespace Polis.Sim
{
    /// <summary>The generated state of a map. Components that react to a new map read from this.</summary>
    public sealed class MapState
    {
        public HeightMap Heights { get; }

        /// <summary>Flatness in [0, 1] at every vertex; the generator lets less fine detail through where it is high.</summary>
        public FloatMap Flatness { get; }

        /// <summary>Map size in tiles.</summary>
        public MapSize Size => Heights.Size;

        /// <summary>Highest vertex height in the map, in height units.</summary>
        public int MaxHeight { get; }

        public MapState(HeightMap heights, FloatMap flatness)
        {
            Heights = heights ?? throw new ArgumentNullException(nameof(heights));
            Flatness = flatness ?? throw new ArgumentNullException(nameof(flatness));
            foreach (int h in heights.Data)
                if (h > MaxHeight) MaxHeight = h;
        }
    }
}
