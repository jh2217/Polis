using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class HeightMapTests
    {
        [Test]
        public void HeightMap_IndexAndAccessor_RoundTrip()
        {
            var f = new HeightMap(5, 3);
            f[4, 2] = 7f;
            Assert.AreEqual(14, f.Index(4, 2));
            Assert.AreEqual(7f, f.Data[14]);
            Assert.IsTrue(f.InBounds(4, 2));
            Assert.IsFalse(f.InBounds(5, 0));
            Assert.IsFalse(f.InBounds(0, -1));
        }
    }
}
