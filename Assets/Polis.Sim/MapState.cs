using System;

namespace Polis.Sim
{
    /// <summary>The generated state of a map. Components that react to a new map read from this.</summary>
    public sealed class MapState
    {
        public HeightMap Heights { get; }

        /// <summary>Flatness in [0, 1] at every vertex; the generator lets less fine detail through where it is high.</summary>
        public FloatMap Flatness { get; }

        /// <summary>Temperature in [0, 1] at every vertex, from cold to hot.</summary>
        public FloatMap Temperature { get; }

        /// <summary>Moisture in [0, 1] at every vertex, from dry to wet.</summary>
        public FloatMap Moisture { get; }

        /// <summary>Map size in tiles.</summary>
        public MapSize Size => Heights.Size;

        /// <summary>Highest vertex height in the map, in height units.</summary>
        public int MaxHeight { get; }

        public MapState(HeightMap heights, FloatMap flatness, FloatMap temperature, FloatMap moisture)
        {
            Heights = heights ?? throw new ArgumentNullException(nameof(heights));
            Flatness = flatness ?? throw new ArgumentNullException(nameof(flatness));
            Temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
            Moisture = moisture ?? throw new ArgumentNullException(nameof(moisture));
            foreach (int h in heights.Data)
                if (h > MaxHeight) MaxHeight = h;
        }
    }
}
