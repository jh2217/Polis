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
            s.Elevation.HillSpacing = -5f;
            s.Climate.AirHumidity = 5f;

            var v = s.Validated();

            Assert.AreEqual(ElevationSettings.MinFeatureSize, v.Elevation.HillSpacing);
            Assert.AreEqual(1f, v.Climate.AirHumidity);
        }
    }
}
