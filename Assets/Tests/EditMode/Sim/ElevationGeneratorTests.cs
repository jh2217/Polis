using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class ElevationGeneratorTests
    {
        private static ElevationGenerator.Result Generate(MapSize size, SimRng rng, ElevationSettings settings) =>
            ElevationGenerator.Generate(size, rng, settings, CityScale.Grid);

        [Test]
        public void Generate_CoarserCells_SampleTheSameTerrainInMetres()
        {
            // Cells twice as wide: every vertex of the coarse map lies where every other vertex of the fine map does.
            var s = ElevationSettings.Default;
            s.UseSeed = true;
            s.Seed = 4;

            var fine = ElevationGenerator.Generate(new MapSize(32, 32), new SimRng(1), s, new GridScale(10f, 2f));
            var coarse = ElevationGenerator.Generate(new MapSize(16, 16), new SimRng(1), s, new GridScale(20f, 2f));

            for (int y = 0; y < coarse.Heights.VertexSize.Y; y++)
            for (int x = 0; x < coarse.Heights.VertexSize.X; x++)
                Assert.AreEqual(fine.Heights[2 * x, 2 * y], coarse.Heights[x, y], $"height at ({x}, {y})");
        }

        [Test]
        public void Generate_SameSeed_Identical_DifferentSeed_Differs()
        {
            var s = ElevationSettings.Default;
            var a = Generate(new MapSize(32, 32), new SimRng(1), s).Heights;
            var b = Generate(new MapSize(32, 32), new SimRng(1), s).Heights;
            var c = Generate(new MapSize(32, 32), new SimRng(2), s).Heights;
            CollectionAssert.AreEqual(a.Data, b.Data);
            CollectionAssert.AreNotEqual(a.Data, c.Data);
        }

        [Test]
        public void Generate_HeightsAreWholeStepsWithinZeroAndMaxElevation_AndVary()
        {
            var s = ElevationSettings.Default;
            var m = Generate(new MapSize(64, 64), new SimRng(5), s).Heights;
            int min = int.MaxValue, max = int.MinValue;
            foreach (int v in m.Data)
            {
                Assert.GreaterOrEqual(v, 0);
                Assert.LessOrEqual(v, CityScale.Grid.HeightStepsIn(s.MaxElevation));
                if (v < min) min = v;
                if (v > max) max = v;
            }
            Assert.Greater(max - min, 2);
        }

        [Test]
        public void Generate_CreatesAHeightForEveryVertex()
        {
            var m = Generate(new MapSize(10, 6), new SimRng(1), ElevationSettings.Default).Heights;
            Assert.AreEqual(11 * 7, m.Data.Length);
        }

        [Test]
        public void Generate_MaxElevationBelowOneStep_StillGivesOneStep()
        {
            var s = ElevationSettings.Default;
            s.MaxElevation = 0f;
            var m = Generate(new MapSize(16, 16), new SimRng(2), s).Heights;
            foreach (int v in m.Data)
                Assert.IsTrue(v == 0 || v == 1, $"unexpected value {v}");
        }

        [Test]
        public void Generate_WithSettingsSeed_IgnoresRngState()
        {
            var s = ElevationSettings.Default;
            s.UseSeed = true;
            s.Seed = 123UL;
            var a = Generate(new MapSize(32, 32), new SimRng(1), s).Heights;
            var b = Generate(new MapSize(32, 32), new SimRng(999), s).Heights;
            CollectionAssert.AreEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_DifferentSettingsSeeds_Differ()
        {
            var s1 = ElevationSettings.Default;
            var s2 = ElevationSettings.Default;
            s1.UseSeed = true;
            s1.Seed = 1UL;
            s2.UseSeed = true;
            s2.Seed = 2UL;
            var a = Generate(new MapSize(32, 32), new SimRng(5), s1).Heights;
            var b = Generate(new MapSize(32, 32), new SimRng(5), s2).Heights;
            CollectionAssert.AreNotEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_WithSettingsSeed_DoesNotConsumeRng()
        {
            var s = ElevationSettings.Default;
            s.UseSeed = true;
            s.Seed = 42UL;
            var used = new SimRng(8);
            Generate(new MapSize(16, 16), used, s);
            Assert.AreEqual(new SimRng(8).NextULong(), used.NextULong());
        }

        [Test]
        public void Generate_Seed_IsTheSettingsSeedWhenUsed_OtherwiseTheOneDrawnFromTheRng()
        {
            var seeded = ElevationSettings.Default;
            seeded.UseSeed = true;
            seeded.Seed = 77UL;
            var unseeded = ElevationSettings.Default;

            Assert.AreEqual(77UL, Generate(new MapSize(8, 8), new SimRng(1), seeded).Seed);
            Assert.AreEqual(new SimRng(5).NextULong(), Generate(new MapSize(8, 8), new SimRng(5), unseeded).Seed);
        }

        [Test]
        public void Generate_UseSeedOff_FallsBackToRng()
        {
            var s = ElevationSettings.Default; // UseSeed == false
            var a = Generate(new MapSize(32, 32), new SimRng(3), s).Heights;
            var b = Generate(new MapSize(32, 32), new SimRng(4), s).Heights;
            CollectionAssert.AreNotEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_FlatnessStrength_ReducesTileToTileRoughness()
        {
            var rough = ElevationSettings.Default;
            rough.UseSeed = true;
            rough.Seed = 7;
            rough.FlatnessStrength = 0f;
            var flat = rough;
            flat.FlatnessStrength = 1f;

            float roughTotal = Roughness(Generate(new MapSize(64, 64), new SimRng(1), rough).Heights);
            float flatTotal = Roughness(Generate(new MapSize(64, 64), new SimRng(1), flat).Heights);

            Assert.Less(flatTotal, roughTotal);
        }

        [Test]
        public void Generate_Flatness_HasOneValueInUnitRangePerVertex_AndVaries()
        {
            var s = ElevationSettings.Default;
            s.PlainSize = 100f; // several plains across the 40-tile map, so the field visibly varies
            var flatness = Generate(new MapSize(40, 24), new SimRng(5), s).Flatness;

            Assert.AreEqual(41 * 25, flatness.Data.Length);
            float min = float.MaxValue, max = float.MinValue;
            foreach (float v in flatness.Data)
            {
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
                if (v < min) min = v;
                if (v > max) max = v;
            }
            Assert.Greater(max - min, 0.1f);
        }

        [Test]
        public void Generate_Flatness_IsDeterministic_AndIgnoresFlatnessStrength()
        {
            var a = ElevationSettings.Default;
            a.UseSeed = true;
            a.Seed = 9;
            a.FlatnessStrength = 0f;
            var b = a;
            b.FlatnessStrength = 1f;

            var flatA = Generate(new MapSize(32, 32), new SimRng(1), a).Flatness;
            var flatB = Generate(new MapSize(32, 32), new SimRng(2), b).Flatness;

            CollectionAssert.AreEqual(flatA.Data, flatB.Data);
        }

        [Test]
        public void Generate_FlatnessStrengthZero_IgnoresFlatnessSettings()
        {
            var a = ElevationSettings.Default;
            a.UseSeed = true;
            a.Seed = 3;
            a.FlatnessStrength = 0f;
            var b = a;
            b.PlainSize = 90f;
            b.FlatnessFromOctave = 0;

            var mapA = Generate(new MapSize(32, 32), new SimRng(1), a).Heights;
            var mapB = Generate(new MapSize(32, 32), new SimRng(1), b).Heights;

            CollectionAssert.AreEqual(mapA.Data, mapB.Data);
        }

        [Test]
        public void Generate_LargerMap_ExtendsTheSameTerrainInsteadOfStretchingIt()
        {
            var s = ElevationSettings.Default;
            s.UseSeed = true;
            s.Seed = 11;

            var small = Generate(new MapSize(32, 32), new SimRng(1), s);
            var large = Generate(new MapSize(64, 64), new SimRng(1), s);

            for (int y = 0; y < small.Heights.VertexSize.Y; y++)
            for (int x = 0; x < small.Heights.VertexSize.X; x++)
            {
                Assert.AreEqual(small.Heights[x, y], large.Heights[x, y], $"height at ({x}, {y})");
                Assert.AreEqual(small.Flatness[x, y], large.Flatness[x, y], $"flatness at ({x}, {y})");
            }
        }

        [Test]
        public void Generate_UseSeedOff_IgnoresSeedValue()
        {
            var s1 = ElevationSettings.Default;
            var s2 = ElevationSettings.Default;
            s1.Seed = 1UL;
            s2.Seed = 2UL;
            var a = Generate(new MapSize(32, 32), new SimRng(5), s1).Heights;
            var b = Generate(new MapSize(32, 32), new SimRng(5), s2).Heights;
            CollectionAssert.AreEqual(a.Data, b.Data);
        }

        [Test]
        public void Generate_OutOfRangeSettings_GiveTheSameMapAsTheirValidatedForm()
        {
            var wild = ElevationSettings.Default;
            wild.FlatnessStrength = 2f;
            wild.MaxElevation = 0f;

            var a = Generate(new MapSize(16, 16), new SimRng(5), wild).Heights;
            var b = Generate(new MapSize(16, 16), new SimRng(5), wild.Validated()).Heights;

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
