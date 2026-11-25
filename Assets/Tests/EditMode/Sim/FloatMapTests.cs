using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class FloatMapTests
    {
        [Test]
        public void Constructor_HasOneValuePerVertex()
        {
            var map = new FloatMap(4, 3);

            Assert.AreEqual(5, map.VertexSize.X);
            Assert.AreEqual(4, map.VertexSize.Y);
            Assert.AreEqual(20, map.Data.Length);
        }

        [Test]
        public void Indexer_ReadsAndWritesTheSameSlotAsIndex()
        {
            var map = new FloatMap(4, 3);

            map[2, 1] = 0.25f;

            Assert.AreEqual(0.25f, map.Data[map.Index(2, 1)]);
            Assert.AreEqual(0.25f, map[2, 1]);
        }

        [Test]
        public void InBounds_CoversVertexGrid()
        {
            var map = new FloatMap(4, 3);

            Assert.IsTrue(map.InBounds(4, 3));
            Assert.IsFalse(map.InBounds(5, 3));
            Assert.IsFalse(map.InBounds(0, -1));
        }
    }
}
