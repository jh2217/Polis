namespace Polis.Sim
{
    /// <summary>
    /// How big the city map is in the real world. These two values are the only ones tied to the size of a city tile
    /// or height step; everything else is given in metres and converted through them. Algorithms shared with the world
    /// map take a GridScale instead of reading these (Grid passes them in).
    /// </summary>
    internal static class CityScale
    {
        /// <summary>The width of one tile in metres: one cottage with its plot occupies a tile.</summary>
        public const float MetersPerTile = 10f;

        /// <summary>
        /// The height of one height step in metres. A one-step slope across a tile is the gentlest slope there is, so
        /// this sets how steep that is (20% at 2 m on 10 m tiles), and every slope stays visible as a slope.
        /// </summary>
        public const float MetersPerHeightUnit = 2f;

        /// <summary>The city map's scale, for algorithms that take one.</summary>
        public static readonly GridScale Grid = new GridScale(MetersPerTile, MetersPerHeightUnit);
    }
}
