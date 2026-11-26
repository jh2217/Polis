namespace Polis.Sim.Grids
{
    /// <summary>Dimensions of a map in tiles (X columns by Y rows). Unrelated to terrain elevation.</summary>
    public readonly struct MapSize
    {
        public readonly int X;
        public readonly int Y;

        public MapSize(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Total number of tiles.</summary>
        public int Count => X * Y;
    }
}
