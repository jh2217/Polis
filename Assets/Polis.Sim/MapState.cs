using System;

namespace Polis.Sim
{
    /// <summary>The generated state of a map. Components that react to a new map read from this.</summary>
    public sealed class MapState
    {
        public HeightMap Heights { get; }

        /// <summary>Map size in tiles.</summary>
        public MapSize Size => Heights.Size;

        /// <summary>Highest vertex height in the map, in height units.</summary>
        public int MaxHeight { get; }

        public MapState(HeightMap heights)
        {
            Heights = heights ?? throw new ArgumentNullException(nameof(heights));
            foreach (int h in heights.Data)
                if (h > MaxHeight) MaxHeight = h;
        }
    }
}
