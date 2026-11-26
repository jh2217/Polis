using NUnit.Framework;
using Polis.Sim.Grids;

namespace Polis.Sim.Tests.Grids
{
    public class FloatMapFiltersTests
    {
        private static FloatMap Constant(int x, int y, float value)
        {
            var map = new FloatMap(x, y);
            for (int i = 0; i < map.Data.Length; i++) map.Data[i] = value;
            return map;
        }

        [Test]
        public void Blurred_ConstantMap_StaysConstant_EvenAtTheEdges()
        {
            var blurred = Constant(8, 6, 0.4f).Blurred(2);

            foreach (float v in blurred.Data) Assert.AreEqual(0.4f, v, 1e-6f);
        }

        [Test]
        public void Blurred_HasTheSameSizeAsTheSource()
        {
            var blurred = new FloatMap(8, 6).Blurred(1);

            Assert.AreEqual(8, blurred.Size.X);
            Assert.AreEqual(6, blurred.Size.Y);
            Assert.AreEqual(9 * 7, blurred.Data.Length);
        }

        [Test]
        public void Blurred_SingleSpike_SpreadsSymmetricallyAndLowersThePeak()
        {
            var source = new FloatMap(10, 10);
            source[5, 5] = 1f;

            var blurred = source.Blurred(1);

            Assert.Less(blurred[5, 5], 1f);
            Assert.Greater(blurred[5, 5], 0f);
            Assert.AreEqual(blurred[4, 5], blurred[6, 5], 1e-6f);
            Assert.AreEqual(blurred[5, 4], blurred[5, 6], 1e-6f);
            Assert.AreEqual(blurred[4, 5], blurred[5, 4], 1e-6f);
            Assert.Greater(blurred[4, 5], 0f);
        }

        [Test]
        public void Blurred_InTheInterior_PreservesTheTotal()
        {
            var source = new FloatMap(20, 20);
            source[10, 10] = 1f; // far enough from the edges that no weight is averaged away

            float total = 0f;
            foreach (float v in source.Blurred(2).Data) total += v;

            Assert.AreEqual(1f, total, 1e-4f);
        }

        [Test]
        public void Blurred_RadiusZeroOrLess_ReturnsAnEqualCopy()
        {
            var source = new FloatMap(4, 4);
            source[2, 3] = 0.7f;

            var copy = source.Blurred(0);

            Assert.AreNotSame(source, copy);
            CollectionAssert.AreEqual(source.Data, copy.Data);
            CollectionAssert.AreEqual(source.Data, source.Blurred(-3).Data);
        }

        [Test]
        public void Blurred_DoesNotChangeTheSource()
        {
            var source = new FloatMap(6, 6);
            source[3, 3] = 1f;

            source.Blurred(2);

            Assert.AreEqual(1f, source[3, 3]);
            Assert.AreEqual(0f, source[2, 3]);
        }
    }
}
