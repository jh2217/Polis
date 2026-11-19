namespace Polis.Sim
{
    /// <summary>Seeded, stateless 2D value noise with fractal (fBm) layering. Output is in [0, 1].</summary>
    public sealed class Noise
    {
        private readonly ulong _seed;

        public Noise(ulong seed)
        {
            _seed = seed;
        }

        /// <summary>Single-octave value noise at (x, y) with bilinear smoothstep interpolation.</summary>
        public float Value(float x, float y)
        {
            int x0 = FloorToInt(x);
            int y0 = FloorToInt(y);
            float tx = Smooth(x - x0);
            float ty = Smooth(y - y0);

            float a = Lattice(x0, y0);
            float b = Lattice(x0 + 1, y0);
            float c = Lattice(x0, y0 + 1);
            float d = Lattice(x0 + 1, y0 + 1);

            float top = a + (b - a) * tx;
            float bottom = c + (d - c) * tx;
            return top + (bottom - top) * ty;
        }

        /// <summary>
        /// Fractal Brownian motion: sums octaves, each at lacunarity x frequency and persistence x amplitude.
        /// Normalized so the result stays in [0, 1].
        /// </summary>
        public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float persistence = 0.5f)
        {
            float sum = 0f, amplitude = 1f, total = 0f, frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(x * frequency, y * frequency) * amplitude;
                total += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return total > 0f ? sum / total : 0f;
        }

        private float Lattice(int ix, int iy)
        {
            ulong h = _seed;
            h ^= (ulong)(uint)ix * 0x9E3779B97F4A7C15UL;
            h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
            h ^= (ulong)(uint)iy * 0x94D049BB133111EBUL;
            h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
            h ^= h >> 31;
            return (h >> 40) * (1f / (1UL << 24));
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static int FloorToInt(float v)
        {
            int i = (int)v;
            return v < i ? i - 1 : i;
        }
    }
}
