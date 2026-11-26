namespace Polis.Sim
{
    /// <summary>
    /// Integer terrain heights defined at the grid's vertices, stored in one flat array.
    /// A map of X by Y tiles has (X + 1) by (Y + 1) vertices; each tile's shape comes from its four corners.
    /// Indexer and Index take vertex coordinates.
    /// </summary>
    public sealed class HeightMap
    {
        /// <summary>Map dimensions in tiles.</summary>
        public readonly MapSize Size;
        /// <summary>Vertex-grid dimensions: one more than Size in each direction.</summary>
        public readonly MapSize VertexSize;
        /// <summary>Height at each vertex, in whole height steps of CityScale.MetersPerHeightUnit.</summary>
        public readonly int[] Data;

        public HeightMap(MapSize size)
        {
            Size = size;
            VertexSize = new MapSize(size.X + 1, size.Y + 1);
            Data = new int[VertexSize.Count];
        }

        public HeightMap(int x, int y) : this(new MapSize(x, y)) { }

        /// <summary>The heights as floats, one per vertex, in a new FloatMap with the same layout.</summary>
        public FloatMap ToFloatMap()
        {
            var floats = new FloatMap(Size);
            for (int i = 0; i < Data.Length; i++)
                floats.Data[i] = Data[i];
            return floats;
        }

        public int Index(int vx, int vy) => vy * VertexSize.X + vx;

        public bool InBounds(int vx, int vy) => vx >= 0 && vy >= 0 && vx < VertexSize.X && vy < VertexSize.Y;

        public int this[int vx, int vy]
        {
            get => Data[Index(vx, vy)];
            set => Data[Index(vx, vy)] = value;
        }
    }
}
