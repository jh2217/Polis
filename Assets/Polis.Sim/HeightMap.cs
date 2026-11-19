namespace Polis.Sim
{
    /// <summary>A grid of floats stored in one flat array.</summary>
    public sealed class HeightMap
    {
        public readonly MapSize Size;
        public readonly float[] Data;

        public HeightMap(MapSize size)
        {
            Size = size;
            Data = new float[size.Count];
        }

        public HeightMap(int x, int y)
        {
            Size = new MapSize(x, y);
            Data = new float[Size.Count];
        }

        public int Index(int x, int y) => y * Size.X + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size.X && y < Size.Y;

        public float this[int x, int y]
        {
            get => Data[Index(x, y)];
            set => Data[Index(x, y)] = value;
        }
    }
}
