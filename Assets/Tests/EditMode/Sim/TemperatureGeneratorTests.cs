using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class TemperatureGeneratorTests
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
        public void Generate_HasOneTemperaturePerVertex_AndTheIndexIsInUnitRange()
        {
            var result = TemperatureGenerator.Generate(SlopedMap(24, 10), ClimateSettings.Default);

            Assert.AreEqual(25 * 25, result.Temperature.Data.Length);
            Assert.AreEqual(25 * 25, result.TemperatureIndex.Data.Length);
            foreach (float v in result.TemperatureIndex.Data) Assert.That(v, Is.InRange(0f, 1f));
        }

        [Test]
        public void Generate_EveryVertexHasTheTemperatureOfItsHeight()
        {
            var map = SlopedMap(24, 10);
            var climate = ClimateSettings.Default;

            var temperature = TemperatureGenerator.Generate(map, climate).Temperature;

            for (int y = 0; y < map.VertexSize.Y; y++)
            for (int x = 0; x < map.VertexSize.X; x++)
                Assert.AreEqual(TemperatureGenerator.TemperatureAtHeight(map[x, y], climate), temperature[x, y], 1e-5f);
        }

        [Test]
        public void Generate_TheIndexIsTheClimateIndexOfTheTemperature()
        {
            var result = TemperatureGenerator.Generate(SlopedMap(24, 10), ClimateSettings.Default);

            CollectionAssert.AreEqual(ClimateIndex.TemperatureIndexOf(result.Temperature).Data, result.TemperatureIndex.Data);
        }

        [Test]
        public void Generate_SeaLevelTemperature_ShiftsEveryVertexByTheSameAmount()
        {
            var map = SlopedMap(24, 10);
            var mild = ClimateSettings.Default;
            var hot = mild;
            hot.SeaLevelTemperature = mild.SeaLevelTemperature + 7f;

            var mildField = TemperatureGenerator.Generate(map, mild).Temperature;
            var hotField = TemperatureGenerator.Generate(map, hot).Temperature;

            for (int i = 0; i < mildField.Data.Length; i++)
                Assert.AreEqual(7f, hotField.Data[i] - mildField.Data[i], 1e-4f);
        }

        [Test]
        public void TemperatureAtHeight_IsTheSeaLevelTemperatureAtHeightZero()
        {
            var climate = ClimateSettings.Default;

            Assert.AreEqual(climate.SeaLevelTemperature, TemperatureGenerator.TemperatureAtHeight(0f, climate), 1e-5f);
        }

        [Test]
        public void TemperatureAtHeight_IsAboutSixAndAHalfDegreesColderOneKilometreUp()
        {
            // A height unit is 5 m, so 200 units are 1 km.
            var climate = ClimateSettings.Default;

            Assert.AreEqual(climate.SeaLevelTemperature - 6.5f, TemperatureGenerator.TemperatureAtHeight(200f, climate), 1e-3f);
        }

        [Test]
        public void TemperatureAtHeight_KeepsCoolingAsTheGroundRises()
        {
            var climate = ClimateSettings.Default;

            float previous = TemperatureGenerator.TemperatureAtHeight(0f, climate);
            for (int height = 10; height <= 1000; height += 10)
            {
                float temperature = TemperatureGenerator.TemperatureAtHeight(height, climate);
                Assert.Less(temperature, previous, $"height {height}");
                previous = temperature;
            }
        }

        [Test]
        public void Generate_OutOfRangeSettings_GiveTheSameMapAsTheirValidatedForm()
        {
            var map = SlopedMap(24, 10);
            var wild = ClimateSettings.Default;
            wild.SeaLevelTemperature = 500f;

            var fromWild = TemperatureGenerator.Generate(map, wild).Temperature;
            var fromValidated = TemperatureGenerator.Generate(map, wild.Validated()).Temperature;

            CollectionAssert.AreEqual(fromValidated.Data, fromWild.Data);
        }
    }
}
