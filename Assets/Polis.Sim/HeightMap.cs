namespace Polis.Sim
{
    /// <summary>A grid of floats stored in one flat array.</summary>
    public sealed class HeightMap
    {
        public readonly int X;
        public readonly int Y;
        public readonly float[] Data;

        public HeightMap(int width, int height)
        {
            X = width;
            Y = height;
            Data = new float[width * height];
        }

        public int Index(int x, int y) => y * X + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < X && y < Y;

        public float this[int x, int y]
        {
            get => Data[Index(x, y)];
            set => Data[Index(x, y)] = value;
        }
    }
}
