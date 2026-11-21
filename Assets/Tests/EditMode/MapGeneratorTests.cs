using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class MapGeneratorTests
    {
        private static MapGenerator.Settings Seeded(ulong seed)
        {
            var s = MapGenerator.Settings.Default;
            s.Elevation.UseSeed = true;
            s.Elevation.Seed = seed;
            return s;
        }

        [Test]
        public void Generate_AllFieldsMatchTheMapSize()
        {
            var state = MapGenerator.Generate(new MapSize(20, 12), new SimRng(1), MapGenerator.Settings.Default);

            Assert.AreEqual(20, state.Size.X);
            Assert.AreEqual(12, state.Size.Y);
            int vertices = 21 * 13;
            Assert.AreEqual(vertices, state.Heights.Data.Length);
            Assert.AreEqual(vertices, state.Flatness.Data.Length);
            Assert.AreEqual(vertices, state.Temperature.Data.Length);
            Assert.AreEqual(vertices, state.Moisture.Data.Length);
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
            CollectionAssert.AreEqual(a.Moisture.Data, b.Moisture.Data);
            CollectionAssert.AreNotEqual(a.Heights.Data, c.Heights.Data);
            CollectionAssert.AreNotEqual(a.Moisture.Data, c.Moisture.Data);
        }

        [Test]
        public void Generate_UseSeedOff_RngDecidesEveryField()
        {
            var s = MapGenerator.Settings.Default;

            var a = MapGenerator.Generate(new MapSize(24, 24), new SimRng(3), s);
            var b = MapGenerator.Generate(new MapSize(24, 24), new SimRng(4), s);

            CollectionAssert.AreNotEqual(a.Heights.Data, b.Heights.Data);
            CollectionAssert.AreNotEqual(a.Temperature.Data, b.Temperature.Data);
            CollectionAssert.AreNotEqual(a.Moisture.Data, b.Moisture.Data);
        }

        [Test]
        public void Generate_ClimateCoolsTheElevationItWasGiven()
        {
            var warm = Seeded(8);
            warm.Climate.ElevationCooling = 0f;
            var cool = warm;
            cool.Climate.ElevationCooling = 0.5f;

            var warmState = MapGenerator.Generate(new MapSize(24, 24), new SimRng(1), warm);
            var coolState = MapGenerator.Generate(new MapSize(24, 24), new SimRng(1), cool);

            CollectionAssert.AreEqual(warmState.Heights.Data, coolState.Heights.Data);
            float warmTotal = 0f, coolTotal = 0f;
            foreach (float v in warmState.Temperature.Data) warmTotal += v;
            foreach (float v in coolState.Temperature.Data) coolTotal += v;
            Assert.Less(coolTotal, warmTotal);
        }
    }
}
