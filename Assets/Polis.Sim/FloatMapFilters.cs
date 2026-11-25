using System;

namespace Polis.Sim
{
    /// <summary>Operations that produce a new FloatMap from an existing one.</summary>
    internal static class FloatMapFilters
    {
        /// <summary>
        /// A copy blurred by two passes of a box filter with the given radius in vertices (radius 0 or less is an
        /// unchanged copy). Near the edges each vertex averages the neighbors that are in bounds.
        /// </summary>
        public static FloatMap Blurred(this FloatMap source, int radius)
        {
            if (radius <= 0)
            {
                var copy = new FloatMap(source.Size);
                Array.Copy(source.Data, copy.Data, source.Data.Length);
                return copy;
            }
            return BoxBlur(BoxBlur(source, radius), radius);
        }

        private static FloatMap BoxBlur(FloatMap source, int radius)
        {
            var size = source.VertexSize;
            var horizontal = new FloatMap(source.Size);
            for (int y = 0; y < size.Y; y++)
            for (int x = 0; x < size.X; x++)
            {
                int from = Math.Max(0, x - radius);
                int to = Math.Min(size.X - 1, x + radius);
                float sum = 0f;
                for (int i = from; i <= to; i++) sum += source[i, y];
                horizontal[x, y] = sum / (to - from + 1);
            }

            var result = new FloatMap(source.Size);
            for (int y = 0; y < size.Y; y++)
            for (int x = 0; x < size.X; x++)
            {
                int from = Math.Max(0, y - radius);
                int to = Math.Min(size.Y - 1, y + radius);
                float sum = 0f;
                for (int i = from; i <= to; i++) sum += horizontal[x, i];
                result[x, y] = sum / (to - from + 1);
            }
            return result;
        }
    }
}
