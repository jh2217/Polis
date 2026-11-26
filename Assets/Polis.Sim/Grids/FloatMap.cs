namespace Polis.Sim.Grids
{
    /// <summary>
    /// A float value at every vertex of the grid, stored in one flat array with the same layout as HeightMap:
    /// a map of X by Y tiles has (X + 1) by (Y + 1) vertices. Indexer and Index take vertex coordinates.
    /// </summary>
    public sealed class FloatMap
    {
        /// <summary>Map dimensions in tiles.</summary>
        public readonly MapSize Size;
        /// <summary>Vertex-grid dimensions: one more than Size in each direction.</summary>
        public readonly MapSize VertexSize;
        public readonly float[] Data;

        public FloatMap(MapSize size)
        {
            Size = size;
            VertexSize = new MapSize(size.X + 1, size.Y + 1);
            Data = new float[VertexSize.Count];
        }

        public FloatMap(int x, int y) : this(new MapSize(x, y)) { }

        public int Index(int vx, int vy) => vy * VertexSize.X + vx;

        public bool InBounds(int vx, int vy) => vx >= 0 && vy >= 0 && vx < VertexSize.X && vy < VertexSize.Y;

        public float this[int vx, int vy]
        {
            get => Data[Index(vx, vy)];
            set => Data[Index(vx, vy)] = value;
        }
    }
}
