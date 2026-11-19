using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class NoiseTests
    {
        [Test]
        public void Noise_SameSeed_SameValue()
        {
            Assert.AreEqual(new Noise(7).Fbm(1.3f, 4.2f, 4), new Noise(7).Fbm(1.3f, 4.2f, 4));
        }

        [Test]
        public void Noise_Fbm_StaysInUnitRange()
        {
            var n = new Noise(99);
            for (int i = 0; i < 2000; i++)
            {
                float v = n.Fbm(i * 0.37f - 300f, i * 0.91f - 500f, 5);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
            }
        }
    }
}
