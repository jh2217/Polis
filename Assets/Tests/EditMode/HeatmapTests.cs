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
        public void ToTexture_MatchesMapSize_AndPixelColors()
        {
            var map = new HeightMap(4, 3);
            map[2, 1] = 1f;

            var tex = Heatmap.ToTexture(map, Low, Mid, High);
            try
            {
                Assert.AreEqual(4, tex.width);
                Assert.AreEqual(3, tex.height);
                Assert.AreEqual((Color32)High, (Color32)tex.GetPixel(2, 1));
                Assert.AreEqual((Color32)Low, (Color32)tex.GetPixel(0, 0));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }
    }
}
