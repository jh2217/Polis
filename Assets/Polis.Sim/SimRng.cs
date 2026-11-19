namespace Polis.Sim
{
    /// <summary>
    /// Deterministic seeded RNG (xorshift64*). Passed explicitly into every generator;
    /// never use UnityEngine.Random or System.Random inside the simulation.
    /// </summary>
    public sealed class SimRng
    {
        private ulong _state;

        public SimRng(ulong seed)
        {
            // SplitMix64 scramble so small/zero seeds still give well-mixed state.
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            _state = z ^ (z >> 31);
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
        }

        public ulong NextULong()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat() => (NextULong() >> 40) * (1f / (1UL << 24));

        /// <summary>Uniform int in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive) => (int)(NextULong() % (ulong)maxExclusive);
    }
}
