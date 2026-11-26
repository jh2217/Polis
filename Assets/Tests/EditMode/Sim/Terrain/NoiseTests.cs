using NUnit.Framework;
using Polis.Sim.Terrain;

namespace Polis.Sim.Tests.Terrain
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

        [Test]
        public void Fbm_DetailScaleOne_MatchesPlainFbm()
        {
            var n = new Noise(5);
            Assert.AreEqual(n.Fbm(2.7f, 1.1f, 5), n.Fbm(2.7f, 1.1f, 5, 2f, 0.5f, 2, 1f), 1e-6f);
        }

        [Test]
        public void Fbm_DetailScaleZero_KeepsOnlyOctavesBeforeDetailStart()
        {
            var n = new Noise(5);
            Assert.AreEqual(n.Fbm(2.7f, 1.1f, 2), n.Fbm(2.7f, 1.1f, 5, 2f, 0.5f, 2, 0f), 1e-6f);
        }

        [Test]
        public void Fbm_WithDetailScale_StaysInUnitRange()
        {
            var n = new Noise(11);
            for (int i = 0; i < 2000; i++)
            {
                float v = n.Fbm(i * 0.37f - 300f, i * 0.91f - 500f, 5, 2f, 0.5f, 1, (i % 5) / 4f);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
            }
        }
    }
}
