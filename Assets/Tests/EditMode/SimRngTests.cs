using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class SimRngTests
    {
        [Test]
        public void SimRng_SameSeed_SameSequence()
        {
            var a = new SimRng(42);
            var b = new SimRng(42);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void SimRng_DifferentSeeds_Differ()
        {
            Assert.AreNotEqual(new SimRng(1).NextULong(), new SimRng(2).NextULong());
        }

        [Test]
        public void SimRng_NextFloat_InUnitRange()
        {
            var r = new SimRng(0);
            for (int i = 0; i < 10000; i++)
            {
                float f = r.NextFloat();
                Assert.GreaterOrEqual(f, 0f);
                Assert.Less(f, 1f);
            }
        }
    }
}
