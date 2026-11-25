namespace Polis.Sim
{
    /// <summary>How big the city map is in the real world.</summary>
    internal static class WorldScale
    {
        /// <summary>The width of one tile in metres: one cottage with its plot occupies a tile.</summary>
        public const float MetersPerTile = 10f;

        /// <summary>The height of one height unit in metres: half a tile's width.</summary>
        public const float MetersPerHeightUnit = MetersPerTile / 2f;
    }
}
