using System;
using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class MapStateTests
    {
        private static MapState StateOf(HeightMap heights) => new MapState(heights, new FloatMap(heights.Size));

        [Test]
        public void Size_IsTheHeightMapsSizeInTiles()
        {
            var state = StateOf(new HeightMap(4, 3));

            Assert.AreEqual(4, state.Size.X);
            Assert.AreEqual(3, state.Size.Y);
        }

        [Test]
        public void MaxHeight_IsTheHighestVertex()
        {
            var map = new HeightMap(2, 2);
            map[1, 2] = 7;
            map[0, 0] = 3;

            Assert.AreEqual(7, StateOf(map).MaxHeight);
        }

        [Test]
        public void MaxHeight_FlatZeroMap_IsZero()
        {
            Assert.AreEqual(0, StateOf(new HeightMap(2, 2)).MaxHeight);
        }

        [Test]
        public void Constructor_KeepsTheGivenMaps()
        {
            var heights = new HeightMap(2, 2);
            var flatness = new FloatMap(2, 2);

            var state = new MapState(heights, flatness);

            Assert.AreSame(heights, state.Heights);
            Assert.AreSame(flatness, state.Flatness);
        }

        [Test]
        public void Constructor_NullMaps_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new MapState(null, new FloatMap(2, 2)));
            Assert.Throws<ArgumentNullException>(() => new MapState(new HeightMap(2, 2), null));
        }
    }
}
