using System;

namespace Polis.Sim.Grids
{
    /// <summary>
    /// The real size of a map's cells and height steps. Algorithms shared by the world map and the city map take one,
    /// so they work at either scale; the city map's is CityScale.Grid.
    /// </summary>
    internal readonly struct GridScale
    {
        /// <summary>The width of one cell in metres; vertices are one cell apart.</summary>
        public readonly float MetersPerCell;
        /// <summary>The height of one height step in metres.</summary>
        public readonly float MetersPerHeightStep;

        public GridScale(float metersPerCell, float metersPerHeightStep)
        {
            MetersPerCell = metersPerCell;
            MetersPerHeightStep = metersPerHeightStep;
        }

        /// <summary>The whole number of height steps nearest to the given height in metres.</summary>
        public int HeightStepsIn(float meters) => (int)Math.Round(meters / MetersPerHeightStep);
    }
}
