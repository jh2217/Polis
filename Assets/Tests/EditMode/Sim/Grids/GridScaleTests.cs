using NUnit.Framework;
using Polis.Sim.Grids;

namespace Polis.Sim.Tests.Grids
{
    public class GridScaleTests
    {
        [Test]
        public void HeightStepsIn_IsZeroForNoHeight_AndOneForOneStep()
        {
            var scale = new GridScale(10f, 2f);

            Assert.AreEqual(0, scale.HeightStepsIn(0f));
            Assert.AreEqual(1, scale.HeightStepsIn(2f));
        }

        [Test]
        public void HeightStepsIn_RoundsToTheNearestWholeStep()
        {
            var scale = new GridScale(10f, 2f);

            Assert.AreEqual(10, scale.HeightStepsIn(20.8f));
            Assert.AreEqual(11, scale.HeightStepsIn(21.2f));
        }

        [Test]
        public void HeightStepsIn_FollowsTheStepSize()
        {
            Assert.AreEqual(50, new GridScale(10f, 2f).HeightStepsIn(100f));
            Assert.AreEqual(20, new GridScale(10f, 5f).HeightStepsIn(100f));
        }
    }
}
