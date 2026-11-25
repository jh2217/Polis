using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class ElevationSettingsTests
    {
        [Test]
        public void Validated_BringsEveryValueIntoItsRange()
        {
            var s = ElevationSettings.Default;
            s.HillSpacing = -1f;
            s.Octaves = 0;
            s.MaxElevation = 0f;
            s.PlainSize = -1f;
            s.FlatnessStrength = 2f;
            s.FlatnessFromOctave = -1;

            var v = s.Validated();

            Assert.AreEqual(WorldScale.MetersPerTile, v.HillSpacing);
            Assert.AreEqual(1, v.Octaves);
            Assert.AreEqual(WorldScale.MetersPerHeightUnit, v.MaxElevation);
            Assert.AreEqual(WorldScale.MetersPerTile, v.PlainSize);
            Assert.AreEqual(1f, v.FlatnessStrength);
            Assert.AreEqual(0, v.FlatnessFromOctave);
        }

        [Test]
        public void Validated_LeavesDefaultsUnchanged()
        {
            var d = ElevationSettings.Default;

            var v = d.Validated();

            Assert.AreEqual(d.HillSpacing, v.HillSpacing);
            Assert.AreEqual(d.Octaves, v.Octaves);
            Assert.AreEqual(d.MaxElevation, v.MaxElevation);
            Assert.AreEqual(d.PlainSize, v.PlainSize);
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
