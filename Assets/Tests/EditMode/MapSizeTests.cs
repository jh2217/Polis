using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class MapSizeTests
    {
        [Test]
        public void MapSize_StoresXAndY()
        {
            var size = new MapSize(5, 3);
            Assert.AreEqual(5, size.X);
            Assert.AreEqual(3, size.Y);
        }

        [Test]
        public void MapSize_Count_IsTileTotal()
        {
            Assert.AreEqual(15, new MapSize(5, 3).Count);
        }
    }
}
