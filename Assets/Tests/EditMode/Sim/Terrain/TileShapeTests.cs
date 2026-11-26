using NUnit.Framework;
using Polis.Sim.Grids;
using Polis.Sim.Terrain;

namespace Polis.Sim.Tests.Terrain
{
    public class TileShapeTests
    {
        [Test]
        public void MaxSlope_IsTheMostHeightStepsThatFitInTheCliffRise()
        {
            Assert.LessOrEqual(TileShape.MaxSlope * CityScale.MetersPerHeightUnit, TileShape.CliffRiseMeters);
            Assert.Greater((TileShape.MaxSlope + 1) * CityScale.MetersPerHeightUnit, TileShape.CliffRiseMeters);
        }

        [Test]
        public void HeightUnitInTileWidths_KeepsTrueProportions()
        {
            Assert.AreEqual(CityScale.MetersPerHeightUnit, TileShape.HeightUnitInTileWidths * CityScale.MetersPerTile, 1e-5f);
        }

        [Test]
        public void Of_SlopeWithinLimit_IsUnchangedAndNotACliff()
        {
            var map = new HeightMap(1, 1);
            map[0, 0] = 0;
            map[1, 0] = TileShape.MaxSlope / 2;
            map[0, 1] = TileShape.MaxSlope;
            map[1, 1] = TileShape.MaxSlope;

            var s = TileShape.Of(map, 0, 0);

            Assert.IsFalse(s.IsCliff);
            Assert.AreEqual(0, s.H00);
            Assert.AreEqual(TileShape.MaxSlope / 2, s.H10);
            Assert.AreEqual(TileShape.MaxSlope, s.H01);
            Assert.AreEqual(TileShape.MaxSlope, s.H11);
        }

        [Test]
        public void Of_DropBeyondLimit_IsACliff_AndLowCornersAreClampedToMaxMinusLimit()
        {
            var map = new HeightMap(1, 1);
            map[0, 0] = 0;
            map[1, 0] = 0;
            map[0, 1] = TileShape.MaxSlope * 3;
            map[1, 1] = TileShape.MaxSlope * 3 - 1;

            var s = TileShape.Of(map, 0, 0);

            Assert.IsTrue(s.IsCliff);
            Assert.AreEqual(TileShape.MaxSlope * 2, s.H00);
            Assert.AreEqual(TileShape.MaxSlope * 2, s.H10);
            Assert.AreEqual(TileShape.MaxSlope * 3, s.H01);
            Assert.AreEqual(TileShape.MaxSlope * 3 - 1, s.H11);
        }

        [Test]
        public void Of_DropExactlyAtLimit_IsNotACliff()
        {
            var map = new HeightMap(1, 1);
            map[1, 1] = TileShape.MaxSlope;

            Assert.IsFalse(TileShape.Of(map, 0, 0).IsCliff);
        }

        [Test]
        public void Of_DiagonalDropBeyondLimit_IsACliff()
        {
            var map = new HeightMap(1, 1);
            map[0, 0] = 0;
            map[1, 0] = TileShape.MaxSlope;
            map[0, 1] = TileShape.MaxSlope;
            map[1, 1] = TileShape.MaxSlope * 2; // every edge drops by MaxSlope, but corner to corner by twice that

            Assert.IsTrue(TileShape.Of(map, 0, 0).IsCliff);
        }
    }
}
