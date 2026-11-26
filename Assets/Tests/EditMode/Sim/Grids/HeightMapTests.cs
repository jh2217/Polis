using NUnit.Framework;
using Polis.Sim.Grids;

namespace Polis.Sim.Tests.Grids
{
    public class HeightMapTests
    {
        [Test]
        public void HeightMap_HasOneMoreVertexThanTilesInEachDirection()
        {
            var m = new HeightMap(5, 3);
            Assert.AreEqual(5, m.Size.X);
            Assert.AreEqual(3, m.Size.Y);
            Assert.AreEqual(6, m.VertexSize.X);
            Assert.AreEqual(4, m.VertexSize.Y);
            Assert.AreEqual(24, m.Data.Length);
        }

        [Test]
        public void HeightMap_IndexAndAccessor_RoundTrip()
        {
            var m = new HeightMap(5, 3);
            m[5, 3] = 7; // the far corner vertex
            Assert.AreEqual(23, m.Index(5, 3));
            Assert.AreEqual(7, m.Data[23]);
        }

        [Test]
        public void HeightMap_InBounds_CoversVertices()
        {
            var m = new HeightMap(5, 3);
            Assert.IsTrue(m.InBounds(5, 3));
            Assert.IsFalse(m.InBounds(6, 0));
            Assert.IsFalse(m.InBounds(0, 4));
            Assert.IsFalse(m.InBounds(0, -1));
        }

        [Test]
        public void HeightMap_MapSizeConstructor_MatchesIntConstructor()
        {
            var m = new HeightMap(new MapSize(5, 3));
            Assert.AreEqual(24, m.Data.Length);
        }

        [Test]
        public void ToFloatMap_CopiesEveryHeightAtTheSameVertex()
        {
            var m = new HeightMap(5, 3);
            m[0, 0] = 2;
            m[5, 3] = 7;

            var floats = m.ToFloatMap();

            Assert.AreEqual(5, floats.Size.X);
            Assert.AreEqual(3, floats.Size.Y);
            Assert.AreEqual(2f, floats[0, 0]);
            Assert.AreEqual(7f, floats[5, 3]);
            Assert.AreEqual(0f, floats[2, 1]);
        }

        [Test]
        public void ToFloatMap_ReturnsACopy()
        {
            var m = new HeightMap(2, 2);

            var floats = m.ToFloatMap();
            floats[1, 1] = 9f;

            Assert.AreEqual(0, m[1, 1]);
        }
    }
}
