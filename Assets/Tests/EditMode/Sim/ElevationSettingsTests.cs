using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class ElevationSettingsTests
    {
        [Test]
        public void Validated_BringsEveryValueIntoItsRange()
        {
            var s = ElevationSettings.Default;
            s.Scale = -1f;
            s.Octaves = 0;
            s.MaxHeight = 0;
            s.FlatnessScale = -1f;
            s.FlatnessStrength = 2f;
            s.FlatnessFromOctave = -1;

            var v = s.Validated();

            Assert.AreEqual(0f, v.Scale);
            Assert.AreEqual(1, v.Octaves);
            Assert.AreEqual(1, v.MaxHeight);
            Assert.AreEqual(0f, v.FlatnessScale);
            Assert.AreEqual(1f, v.FlatnessStrength);
            Assert.AreEqual(0, v.FlatnessFromOctave);
        }

        [Test]
        public void Validated_LeavesDefaultsUnchanged()
        {
            var d = ElevationSettings.Default;

            var v = d.Validated();

            Assert.AreEqual(d.Scale, v.Scale);
            Assert.AreEqual(d.Octaves, v.Octaves);
            Assert.AreEqual(d.MaxHeight, v.MaxHeight);
            Assert.AreEqual(d.FlatnessScale, v.FlatnessScale);
            Assert.AreEqual(d.FlatnessStrength, v.FlatnessStrength);
            Assert.AreEqual(d.FlatnessFromOctave, v.FlatnessFromOctave);
        }

        [Test]
        public void Validated_ReturnsACopy()
        {
            var s = ElevationSettings.Default;
            s.FlatnessStrength = 2f;

            s.Validated();

            Assert.AreEqual(2f, s.FlatnessStrength);
        }
    }
}
