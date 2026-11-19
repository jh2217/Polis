using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class ElevationGeneratorTests
    {
        [Test]
        public void Generate_SameSeed_Identical_DifferentSeed_Differs()
        {
            var s = ElevationGenerator.Settings.Default;
            var a = ElevationGenerator.Generate(32, 32, new SimRng(1), s);
            var b = ElevationGenerator.Generate(32, 32, new SimRng(1), s);
            var c = ElevationGenerator.Generate(32, 32, new SimRng(2), s);
            CollectionAssert.AreEqual(a.Data, b.Data);
            CollectionAssert.AreNotEqual(a.Data, c.Data);
        }

        [Test]
        public void Generate_ValuesInUnitRange_AndHasVariation()
        {
            var m = ElevationGenerator.Generate(64, 64, new SimRng(5), ElevationGenerator.Settings.Default);
            float min = float.MaxValue, max = float.MinValue;
            foreach (float v in m.Data)
            {
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
                if (v < min) min = v;
                if (v > max) max = v;
            }
            Assert.Greater(max - min, 0.1f);
        }

        [Test]
        public void Generate_WithSettingsSeed_IgnoresRngState()
        {
            var s = ElevationGenerator.Settings.Default;
            s.Seed = 123UL;
            var a = ElevationGenerator.Generate(32, 32, new SimRng(1), s);
            var b = ElevationGenerator.Generate(32, 32, new SimRng(999), s);
            CollectionAssert.AreEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_DifferentSettingsSeeds_Differ()
        {
            var s1 = ElevationGenerator.Settings.Default;
            var s2 = ElevationGenerator.Settings.Default;
            s1.Seed = 1UL;
            s2.Seed = 2UL;
            var a = ElevationGenerator.Generate(32, 32, new SimRng(5), s1);
            var b = ElevationGenerator.Generate(32, 32, new SimRng(5), s2);
            CollectionAssert.AreNotEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_WithSettingsSeed_DoesNotConsumeRng()
        {
            var s = ElevationGenerator.Settings.Default;
            s.Seed = 42UL;
            var used = new SimRng(8);
            ElevationGenerator.Generate(16, 16, used, s);
            Assert.AreEqual(new SimRng(8).NextULong(), used.NextULong());
        }

        [Test]
        public void Generate_NullSeed_FallsBackToRng()
        {
            var s = ElevationGenerator.Settings.Default; // Seed == null
            var a = ElevationGenerator.Generate(32, 32, new SimRng(3), s);
            var b = ElevationGenerator.Generate(32, 32, new SimRng(4), s);
            CollectionAssert.AreNotEqual(a.Data, b.Data);
        }
    }
}
