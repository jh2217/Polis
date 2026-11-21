using System;
using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class MapStateTests
    {
        [Test]
        public void Size_IsTheHeightMapsSizeInTiles()
        {
            var state = new MapState(new HeightMap(4, 3));

            Assert.AreEqual(4, state.Size.X);
            Assert.AreEqual(3, state.Size.Y);
        }

        [Test]
        public void MaxHeight_IsTheHighestVertex()
        {
            var map = new HeightMap(2, 2);
            map[1, 2] = 7;
            map[0, 0] = 3;

            Assert.AreEqual(7, new MapState(map).MaxHeight);
        }

        [Test]
        public void MaxHeight_FlatZeroMap_IsZero()
        {
            Assert.AreEqual(0, new MapState(new HeightMap(2, 2)).MaxHeight);
        }

        [Test]
        public void Constructor_NullHeights_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MapState(null));
        }
    }
}
