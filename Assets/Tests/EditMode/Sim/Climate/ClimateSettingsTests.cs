using NUnit.Framework;
using Polis.Sim.Climate;

namespace Polis.Sim.Tests.Climate
{
    public class ClimateSettingsTests
    {
        [Test]
        public void Validated_BringsEveryValueIntoItsRange()
        {
            var s = ClimateSettings.Default;
            s.SeaLevelTemperature = 500f;
            s.AnnualPrecipitation = -100f;
            s.AirHumidity = 3f;

            var v = s.Validated();

            Assert.AreEqual(ClimateSettings.MaxSeaLevelTemperature, v.SeaLevelTemperature);
            Assert.AreEqual(0f, v.AnnualPrecipitation);
            Assert.AreEqual(1f, v.AirHumidity);
        }

        [Test]
        public void Validated_RaisesTooColdASeaLevelToTheMinimum()
        {
            var s = ClimateSettings.Default;
            s.SeaLevelTemperature = -500f;

            Assert.AreEqual(ClimateSettings.MinSeaLevelTemperature, s.Validated().SeaLevelTemperature);
        }

        [Test]
        public void Validated_LeavesDefaultsUnchanged()
        {
            var d = ClimateSettings.Default;

            var v = d.Validated();

            Assert.AreEqual(d.SeaLevelTemperature, v.SeaLevelTemperature);
            Assert.AreEqual(d.AnnualPrecipitation, v.AnnualPrecipitation);
            Assert.AreEqual(d.AirHumidity, v.AirHumidity);
        }

        [Test]
        public void Validated_ReturnsACopy()
        {
            var s = ClimateSettings.Default;
            s.AirHumidity = 3f;

            s.Validated();

            Assert.AreEqual(3f, s.AirHumidity);
        }
    }
}
