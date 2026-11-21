using NUnit.Framework;
using Polis.Unity;
using UnityEngine;

namespace Polis.Sim.Tests
{
    public class HeatmapTests
    {
        private static readonly Color Low = Color.blue;
        private static readonly Color Mid = Color.green;
        private static readonly Color High = Color.red;

        [Test]
        public void Evaluate_ClampsOutOfRangeInput()
        {
            Assert.AreEqual(Heatmap.Evaluate(0f, Low, Mid, High), Heatmap.Evaluate(-5f, Low, Mid, High));
            Assert.AreEqual(Heatmap.Evaluate(1f, Low, Mid, High), Heatmap.Evaluate(5f, Low, Mid, High));
        }

        [Test]
        public void Evaluate_HitsSuppliedColorsAtStops()
        {
            Assert.AreEqual(Low, Heatmap.Evaluate(0f, Low, Mid, High));
            Assert.AreEqual(Mid, Heatmap.Evaluate(0.5f, Low, Mid, High));
            Assert.AreEqual(High, Heatmap.Evaluate(1f, Low, Mid, High));
        }

        [Test]
        public void ToTexture_Heights_AreColoredByHeightOverMax()
        {
            var map = new HeightMap(4, 3); // 5 x 4 vertices
            map[2, 1] = 10;
            map[3, 2] = 5;

            var tex = Heatmap.ToTexture(map, 10, Low, Mid, High);
            try
            {
                Assert.AreEqual(5, tex.width);
                Assert.AreEqual(4, tex.height);
                Assert.AreEqual((Color32)High, (Color32)tex.GetPixel(2, 1));
                Assert.AreEqual((Color32)Mid, (Color32)tex.GetPixel(3, 2));
                Assert.AreEqual((Color32)Low, (Color32)tex.GetPixel(0, 0));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void ToTexture_Heights_NonPositiveMax_IsAllLow()
        {
            var map = new HeightMap(2, 2);
            map[1, 1] = 4;

            var tex = Heatmap.ToTexture(map, 0, Low, Mid, High);
            try
            {
                Assert.AreEqual((Color32)Low, (Color32)tex.GetPixel(1, 1));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void ToTexture_HasOnePixelPerVertex_ColoredByFieldValue()
        {
            var field = new FloatMap(4, 3); // 5 x 4 vertices
            field[2, 1] = 1f;
            field[3, 2] = 0.5f;

            var tex = Heatmap.ToTexture(field, Low, Mid, High);
            try
            {
                Assert.AreEqual(5, tex.width);
                Assert.AreEqual(4, tex.height);
                Assert.AreEqual((Color32)High, (Color32)tex.GetPixel(2, 1));
                Assert.AreEqual((Color32)Mid, (Color32)tex.GetPixel(3, 2));
                Assert.AreEqual((Color32)Low, (Color32)tex.GetPixel(0, 0));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }
    }
}
