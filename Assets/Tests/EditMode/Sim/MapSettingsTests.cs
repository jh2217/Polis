using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class MapSettingsTests
    {
        [Test]
        public void Default_CombinesThePartsDefaults()
        {
            var d = MapSettings.Default;

            Assert.AreEqual(ElevationSettings.Default.MaxElevation, d.Elevation.MaxElevation);
            Assert.AreEqual(ClimateSettings.Default.SeaLevelTemperature, d.Climate.SeaLevelTemperature);
            Assert.AreEqual(ClimateSettings.Default.AnnualPrecipitation, d.Climate.AnnualPrecipitation);
        }

        [Test]
        public void Validated_ValidatesEveryPartsSettings()
        {
            var s = MapSettings.Default;
            s.Elevation.MaxElevation = 0f;
            s.Climate.AirHumidity = 5f;

            var v = s.Validated();

            Assert.AreEqual(WorldScale.MetersPerHeightUnit, v.Elevation.MaxElevation);
            Assert.AreEqual(1f, v.Climate.AirHumidity);
        }
    }
}
