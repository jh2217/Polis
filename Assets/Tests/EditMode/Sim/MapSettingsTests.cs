using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class MapSettingsTests
    {
        [Test]
        public void Default_CombinesThePartsDefaults()
        {
            var d = MapSettings.Default;

            Assert.AreEqual(ElevationSettings.Default.MaxHeight, d.Elevation.MaxHeight);
            Assert.AreEqual(ClimateSettings.Default.SeaLevelTemperature, d.Climate.SeaLevelTemperature);
            Assert.AreEqual(ClimateSettings.Default.AnnualPrecipitation, d.Climate.AnnualPrecipitation);
        }

        [Test]
        public void Validated_ValidatesEveryPartsSettings()
        {
            var s = MapSettings.Default;
            s.Elevation.MaxHeight = 0;
            s.Climate.AirHumidity = 5f;

            var v = s.Validated();

            Assert.AreEqual(1, v.Elevation.MaxHeight);
            Assert.AreEqual(1f, v.Climate.AirHumidity);
        }
    }
}
