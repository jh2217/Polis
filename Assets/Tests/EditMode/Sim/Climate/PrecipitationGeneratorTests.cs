using NUnit.Framework;
using Polis.Sim.Climate;
using Polis.Sim.Grids;

namespace Polis.Sim.Tests.Climate
{
    public class PrecipitationGeneratorTests
    {
        // A temperature field 8 x 4 tiles at one temperature everywhere.
        private static FloatMap UniformTemperature(float temperature)
        {
            var field = new FloatMap(8, 4);
            for (int i = 0; i < field.Data.Length; i++) field.Data[i] = temperature;
            return field;
        }

        [Test]
        public void Generate_GivesEveryVertexTheRegionsPrecipitation()
        {
            var climate = ClimateSettings.Default;
            climate.AnnualPrecipitation = 1234f;

            var precipitation = PrecipitationGenerator.Generate(UniformTemperature(10f), climate).Precipitation;

            Assert.AreEqual(9 * 5, precipitation.Data.Length);
            foreach (float v in precipitation.Data) Assert.AreEqual(1234f, v);
        }

        [Test]
        public void Generate_AWetterRegionGetsAHigherPrecipitationIndex()
        {
            var dry = ClimateSettings.Default;
            dry.AnnualPrecipitation = 300f;
            var wet = dry;
            wet.AnnualPrecipitation = 2000f;

            var dryIndex = PrecipitationGenerator.Generate(UniformTemperature(10f), dry).PrecipitationIndex;
            var wetIndex = PrecipitationGenerator.Generate(UniformTemperature(10f), wet).PrecipitationIndex;

            Assert.Greater(wetIndex[0, 0], dryIndex[0, 0]);
        }

        [Test]
        public void Generate_SurfaceHumidity_IsTheAirHumidityWhereTheGroundIsAtSeaLevelTemperature()
        {
            var climate = ClimateSettings.Default;

            var surfaceHumidity = PrecipitationGenerator.Generate(UniformTemperature(climate.SeaLevelTemperature), climate).SurfaceHumidity;

            foreach (float v in surfaceHumidity.Data) Assert.AreEqual(climate.AirHumidity, v, 1e-5f);
        }

        [Test]
        public void Generate_SurfaceHumidity_IsHigherOverColderGround()
        {
            var climate = ClimateSettings.Default;

            var overCold = PrecipitationGenerator.Generate(UniformTemperature(climate.SeaLevelTemperature - 3f), climate).SurfaceHumidity;
            var overWarm = PrecipitationGenerator.Generate(UniformTemperature(climate.SeaLevelTemperature + 3f), climate).SurfaceHumidity;

            Assert.Greater(overCold[0, 0], overWarm[0, 0]);
        }

        [Test]
        public void Generate_SurfaceHumidity_StaysBetweenZeroAndOne()
        {
            var climate = ClimateSettings.Default;
            climate.AirHumidity = 1f;

            foreach (float temperature in new[] { -40f, 0f, 15f, 40f })
            foreach (float v in PrecipitationGenerator.Generate(UniformTemperature(temperature), climate).SurfaceHumidity.Data)
                Assert.That(v, Is.InRange(0f, 1f), $"{temperature} degrees C");
        }
    }
}
