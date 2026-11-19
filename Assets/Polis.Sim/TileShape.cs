using System;

namespace Polis.Sim
{
    /// <summary>
    /// The surface of one tile, defined by the heights at its four corner vertices.
    /// Corner names are H{dx}{dy}: H10 is the vertex at (tx + 1, ty).
    /// A tile can slope by at most maxSlope height units. If its raw corners differ by more, it is a
    /// cliff tile: its top is clamped to the highest corner minus maxSlope, and the drop to the true
    /// vertex height shows up as a vertical wall along the tile edge (built by the renderer).
    /// </summary>
    public readonly struct TileShape
    {
        /// <summary>Steepest slope a tile may have, in height units; anything steeper becomes a cliff.</summary>
        public const int MaxSlope = 2;

        /// <summary>World width of one height unit, in tile widths.</summary>
        public const float HeightUnitInTileWidths = 0.5f;

        public readonly int H00;
        public readonly int H10;
        public readonly int H01;
        public readonly int H11;

        /// <summary>True when the raw corner heights differ by more than maxSlope.</summary>
        public readonly bool IsCliff;

        private TileShape(int h00, int h10, int h01, int h11, bool isCliff)
        {
            H00 = h00;
            H10 = h10;
            H01 = h01;
            H11 = h11;
            IsCliff = isCliff;
        }

        public static TileShape Of(HeightMap map, int tx, int ty)
        {
            int h00 = map[tx, ty];
            int h10 = map[tx + 1, ty];
            int h01 = map[tx, ty + 1];
            int h11 = map[tx + 1, ty + 1];

            int max = Math.Max(Math.Max(h00, h10), Math.Max(h01, h11));
            int min = Math.Min(Math.Min(h00, h10), Math.Min(h01, h11));
            int floor = max - MaxSlope;

            return new TileShape(
                Math.Max(h00, floor), Math.Max(h10, floor),
                Math.Max(h01, floor), Math.Max(h11, floor),
                max - min > MaxSlope);
        }
    }
}
