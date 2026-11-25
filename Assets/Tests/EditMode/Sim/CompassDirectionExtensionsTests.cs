using System;
using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class CompassDirectionExtensionsTests
    {
        private static readonly CompassDirection[] All = (CompassDirection[])Enum.GetValues(typeof(CompassDirection));

        [TestCase(CompassDirection.East, 1, 0)]
        [TestCase(CompassDirection.NorthEast, 1, 1)]
        [TestCase(CompassDirection.North, 0, 1)]
        [TestCase(CompassDirection.NorthWest, -1, 1)]
        [TestCase(CompassDirection.West, -1, 0)]
        [TestCase(CompassDirection.SouthWest, -1, -1)]
        [TestCase(CompassDirection.South, 0, -1)]
        [TestCase(CompassDirection.SouthEast, 1, -1)]
        public void Step_PointsTowardTheDirection_WithYIncreasingNorth(CompassDirection direction, int dx, int dy)
        {
            Assert.AreEqual((dx, dy), direction.Step());
        }

        [Test]
        public void Step_GivesEveryDirectionADifferentStep()
        {
            var steps = new System.Collections.Generic.HashSet<(int, int)>();
            foreach (var direction in All) steps.Add(direction.Step());

            Assert.AreEqual(All.Length, steps.Count);
        }

        [Test]
        public void Opposite_ReversesTheStep()
        {
            foreach (var direction in All)
            {
                var (dx, dy) = direction.Step();
                Assert.AreEqual((-dx, -dy), direction.Opposite().Step(), direction.ToString());
            }
        }

        [Test]
        public void Opposite_OfTheOpposite_IsTheSameDirection()
        {
            foreach (var direction in All)
                Assert.AreEqual(direction, direction.Opposite().Opposite());
        }

        [Test]
        public void StepLength_IsOneForAxisAlignedAndRootTwoForDiagonalDirections()
        {
            Assert.AreEqual(1f, CompassDirection.East.StepLength());
            Assert.AreEqual(1f, CompassDirection.South.StepLength());
            Assert.AreEqual((float)Math.Sqrt(2), CompassDirection.NorthEast.StepLength(), 1e-6f);
            Assert.AreEqual((float)Math.Sqrt(2), CompassDirection.SouthWest.StepLength(), 1e-6f);
        }

        [Test]
        public void UndefinedDirection_Throws()
        {
            var undefined = (CompassDirection)99;

            Assert.Throws<ArgumentOutOfRangeException>(() => undefined.Step());
            Assert.Throws<ArgumentOutOfRangeException>(() => undefined.StepLength());
            Assert.Throws<ArgumentOutOfRangeException>(() => undefined.Opposite());
        }
    }
}
