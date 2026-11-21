using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class ElevationGeneratorTests
    {
        [Test]
        public void Generate_SameSeed_Identical_DifferentSeed_Differs()
        {
            var s = ElevationGenerator.Settings.Default;
            var a = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(1), s);
            var b = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(1), s);
            var c = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(2), s);
            CollectionAssert.AreEqual(a.Data, b.Data);
            CollectionAssert.AreNotEqual(a.Data, c.Data);
        }

        [Test]
        public void Generate_HeightsAreWholeNumbersWithinZeroAndMaxHeight_AndVary()
        {
            var s = ElevationGenerator.Settings.Default;
            var m = ElevationGenerator.Generate(new MapSize(64, 64), new SimRng(5), s);
            int min = int.MaxValue, max = int.MinValue;
            foreach (int v in m.Data)
            {
                Assert.GreaterOrEqual(v, 0);
                Assert.LessOrEqual(v, s.MaxHeight);
                if (v < min) min = v;
                if (v > max) max = v;
            }
            Assert.Greater(max - min, 2);
        }

        [Test]
        public void Generate_CreatesAHeightForEveryVertex()
        {
            var m = ElevationGenerator.Generate(new MapSize(10, 6), new SimRng(1), ElevationGenerator.Settings.Default);
            Assert.AreEqual(11 * 7, m.Data.Length);
        }

        [Test]
        public void Generate_MaxHeightBelowOne_ClampsToOne()
        {
            var s = ElevationGenerator.Settings.Default;
            s.MaxHeight = 0;
            var m = ElevationGenerator.Generate(new MapSize(16, 16), new SimRng(2), s);
            foreach (int v in m.Data)
                Assert.IsTrue(v == 0 || v == 1, $"unexpected value {v}");
        }

        [Test]
        public void Generate_WithSettingsSeed_IgnoresRngState()
        {
            var s = ElevationGenerator.Settings.Default;
            s.UseSeed = true;
            s.Seed = 123UL;
            var a = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(1), s);
            var b = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(999), s);
            CollectionAssert.AreEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_DifferentSettingsSeeds_Differ()
        {
            var s1 = ElevationGenerator.Settings.Default;
            var s2 = ElevationGenerator.Settings.Default;
            s1.UseSeed = true;
            s1.Seed = 1UL;
            s2.UseSeed = true;
            s2.Seed = 2UL;
            var a = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(5), s1);
            var b = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(5), s2);
            CollectionAssert.AreNotEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_WithSettingsSeed_DoesNotConsumeRng()
        {
            var s = ElevationGenerator.Settings.Default;
            s.UseSeed = true;
            s.Seed = 42UL;
            var used = new SimRng(8);
            ElevationGenerator.Generate(new MapSize(16, 16), used, s);
            Assert.AreEqual(new SimRng(8).NextULong(), used.NextULong());
        }

        [Test]
        public void Generate_UseSeedOff_FallsBackToRng()
        {
            var s = ElevationGenerator.Settings.Default; // UseSeed == false
            var a = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(3), s);
            var b = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(4), s);
            CollectionAssert.AreNotEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_FlatnessStrength_ReducesTileToTileRoughness()
        {
            var rough = ElevationGenerator.Settings.Default;
            rough.UseSeed = true;
            rough.Seed = 7;
            rough.FlatnessStrength = 0f;
            var flat = rough;
            flat.FlatnessStrength = 1f;

            float roughTotal = Roughness(ElevationGenerator.Generate(new MapSize(64, 64), new SimRng(1), rough));
            float flatTotal = Roughness(ElevationGenerator.Generate(new MapSize(64, 64), new SimRng(1), flat));

            Assert.Less(flatTotal, roughTotal);
        }

        [Test]
        public void Generate_FlatnessStrengthZero_IgnoresFlatnessSettings()
        {
            var a = ElevationGenerator.Settings.Default;
            a.UseSeed = true;
            a.Seed = 3;
            a.FlatnessStrength = 0f;
            var b = a;
            b.FlatnessScale = 9f;
            b.FlatnessFromOctave = 0;

            var mapA = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(1), a);
            var mapB = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(1), b);

            CollectionAssert.AreEqual(mapA.Data, mapB.Data);
        }

        [Test]
        public void Generate_UseSeedOff_IgnoresSeedValue()
        {
            var s1 = ElevationGenerator.Settings.Default;
            var s2 = ElevationGenerator.Settings.Default;
            s1.Seed = 1UL;
            s2.Seed = 2UL;
            var a = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(5), s1);
            var b = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(5), s2);
            CollectionAssert.AreEqual(a.Data, b.Data);
        }

        // Sum of height differences between horizontally and vertically adjacent vertices.
        private static float Roughness(HeightMap map)
        {
            float total = 0f;
            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
            {
                if (x + 1 < map.VertexSize.X) total += System.Math.Abs(map[x + 1, y] - map[x, y]);
                if (y + 1 < map.VertexSize.Y) total += System.Math.Abs(map[x, y + 1] - map[x, y]);
            }
            return total;
        }
    }
}
