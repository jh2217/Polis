using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class WorldScaleTests
    {
        [Test]
        public void HeightUnitsIn_IsZeroForNoHeight_AndOneForOneStep()
        {
            Assert.AreEqual(0, WorldScale.HeightUnitsIn(0f));
            Assert.AreEqual(1, WorldScale.HeightUnitsIn(WorldScale.MetersPerHeightUnit));
        }

        [Test]
        public void HeightUnitsIn_RoundsToTheNearestWholeStep()
        {
            Assert.AreEqual(10, WorldScale.HeightUnitsIn(10.4f * WorldScale.MetersPerHeightUnit));
            Assert.AreEqual(11, WorldScale.HeightUnitsIn(10.6f * WorldScale.MetersPerHeightUnit));
        }
    }
}
