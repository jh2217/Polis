using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class ClimateGeneratorTests
    {
        private static HeightMap SlopedMap(int size, int maxHeight)
        {
            var map = new HeightMap(size, size);
            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
                map[x, y] = maxHeight * x / size;
            return map;
        }

        [Test]
        public void Generate_HasOneValueInUnitRangePerVertex_ForBothFields()
        {
            var map = SlopedMap(24, 10);

            var result = ClimateGenerator.Generate(map, 10, 5, ClimateGenerator.Settings.Default);

            Assert.AreEqual(25 * 25, result.Temperature.Data.Length);
            Assert.AreEqual(25 * 25, result.Moisture.Data.Length);
            foreach (float v in result.Temperature.Data) Assert.That(v, Is.InRange(0f, 1f));
            foreach (float v in result.Moisture.Data) Assert.That(v, Is.InRange(0f, 1f));
        }

        [Test]
        public void Generate_SameSeed_Identical_DifferentSeed_Differs()
        {
            var map = SlopedMap(24, 10);
            var s = ClimateGenerator.Settings.Default;

            var a = ClimateGenerator.Generate(map, 10, 1, s);
            var b = ClimateGenerator.Generate(map, 10, 1, s);
            var c = ClimateGenerator.Generate(map, 10, 2, s);

            CollectionAssert.AreEqual(a.Temperature.Data, b.Temperature.Data);
            CollectionAssert.AreEqual(a.Moisture.Data, b.Moisture.Data);
            CollectionAssert.AreNotEqual(a.Temperature.Data, c.Temperature.Data);
            CollectionAssert.AreNotEqual(a.Moisture.Data, c.Moisture.Data);
        }

        [Test]
        public void Generate_TemperatureAndMoisture_AreDifferentFields()
        {
            var result = ClimateGenerator.Generate(SlopedMap(24, 10), 10, 3, ClimateGenerator.Settings.Default);

            CollectionAssert.AreNotEqual(result.Temperature.Data, result.Moisture.Data);
        }

        [Test]
        public void Generate_ElevationCooling_LowersTemperatureWhereTerrainIsHigher()
        {
            var map = SlopedMap(24, 10);
            var warm = ClimateGenerator.Settings.Default;
            warm.ElevationCooling = 0f;
            var cool = warm;
            cool.ElevationCooling = 0.5f;

            var warmField = ClimateGenerator.Generate(map, 10, 4, warm).Temperature;
            var coolField = ClimateGenerator.Generate(map, 10, 4, cool).Temperature;

            for (int i = 0; i < warmField.Data.Length; i++)
                Assert.LessOrEqual(coolField.Data[i], warmField.Data[i]);
            Assert.Less(coolField[24, 5], warmField[24, 5]); // the highest column cools by the full amount
            Assert.AreEqual(warmField[0, 5], coolField[0, 5], 1e-6f); // sea level doesn't cool
        }

        [Test]
        public void Generate_ElevationCooling_DoesNotChangeMoisture()
        {
            var map = SlopedMap(24, 10);
            var a = ClimateGenerator.Settings.Default;
            a.ElevationCooling = 0f;
            var b = a;
            b.ElevationCooling = 0.9f;

            var moistureA = ClimateGenerator.Generate(map, 10, 4, a).Moisture;
            var moistureB = ClimateGenerator.Generate(map, 10, 4, b).Moisture;

            CollectionAssert.AreEqual(moistureA.Data, moistureB.Data);
        }
    }
}
