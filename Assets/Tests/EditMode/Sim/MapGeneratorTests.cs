using NUnit.Framework;
using Polis.Sim.Grids;

namespace Polis.Sim.Tests
{
    public class MapGeneratorTests
    {
        private static MapSettings Seeded(ulong seed)
        {
            var s = MapSettings.Default;
            s.Elevation.UseSeed = true;
            s.Elevation.Seed = seed;
            return s;
        }

        [Test]
        public void Generate_AllFieldsMatchTheMapSize()
        {
            var state = MapGenerator.Generate(new MapSize(20, 12), new SimRng(1), MapSettings.Default);

            Assert.AreEqual(20, state.Size.X);
            Assert.AreEqual(12, state.Size.Y);
            int vertices = 21 * 13;
            Assert.AreEqual(vertices, state.Heights.Data.Length);
            Assert.AreEqual(vertices, state.Flatness.Data.Length);
            Assert.AreEqual(vertices, state.Temperature.Data.Length);
            Assert.AreEqual(vertices, state.PrecipitationIndex.Data.Length);
            Assert.AreEqual(vertices, state.SurfaceHumidity.Data.Length);
        }

        [Test]
        public void Generate_SurfaceHumidity_StaysBetweenZeroAndOne()
        {
            var state = MapGenerator.Generate(new MapSize(20, 12), new SimRng(1), MapSettings.Default);

            foreach (float v in state.SurfaceHumidity.Data) Assert.That(v, Is.InRange(0f, 1f));
        }

        [Test]
        public void Generate_SameSeed_ProducesIdenticalFields_DifferentSeedDiffers()
        {
            var a = MapGenerator.Generate(new MapSize(24, 24), new SimRng(1), Seeded(5));
            var b = MapGenerator.Generate(new MapSize(24, 24), new SimRng(99), Seeded(5));
            var c = MapGenerator.Generate(new MapSize(24, 24), new SimRng(1), Seeded(6));

            CollectionAssert.AreEqual(a.Heights.Data, b.Heights.Data);
            CollectionAssert.AreEqual(a.Flatness.Data, b.Flatness.Data);
            CollectionAssert.AreEqual(a.Temperature.Data, b.Temperature.Data);
            CollectionAssert.AreNotEqual(a.Heights.Data, c.Heights.Data);
            CollectionAssert.AreNotEqual(a.Temperature.Data, c.Temperature.Data);
        }

        [Test]
        public void Generate_UseSeedOff_RngDecidesTheTerrainAndSoTheTemperature()
        {
            var s = MapSettings.Default;

            var a = MapGenerator.Generate(new MapSize(24, 24), new SimRng(3), s);
            var b = MapGenerator.Generate(new MapSize(24, 24), new SimRng(4), s);

            CollectionAssert.AreNotEqual(a.Heights.Data, b.Heights.Data);
            CollectionAssert.AreNotEqual(a.Temperature.Data, b.Temperature.Data);
        }

        [Test]
        public void Generate_HigherGroundIsColder()
        {
            var state = MapGenerator.Generate(new MapSize(24, 24), new SimRng(1), Seeded(8));

            var heights = state.Heights.Data;
            var temperature = state.Temperature.Data;
            for (int i = 0; i < heights.Length; i++)
            for (int j = 0; j < heights.Length; j++)
                if (heights[i] > heights[j])
                    Assert.Less(temperature[i], temperature[j]);
        }

        [Test]
        public void Generate_PrecipitationIsTheRegionsEverywhere()
        {
            var settings = Seeded(8);
            settings.Climate.AnnualPrecipitation = 1500f;

            var state = MapGenerator.Generate(new MapSize(24, 24), new SimRng(1), settings);

            foreach (float v in state.Precipitation.Data) Assert.AreEqual(1500f, v);
        }
    }
}
