using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class PrecipitationSettingsTests
    {
        [Test]
        public void Validated_BringsEveryValueIntoItsRange()
        {
            var s = PrecipitationSettings.Default;
            s.IncomingAirMoisture = 5f;
            s.GroundRainRecycling = 3f;
            s.CapacityGrowth = -1f;
            s.CloudFormationHumidity = 2f;
            s.CloudRainRate = -1f;
            s.AirAnticipationTiles = -1f;
            s.AirLiftBeforeRidge = 2f;
            s.AirRiseRate = -1f;
            s.AirSinkRate = 3f;
            s.RainShadowSmoothing = -2;
            s.PrecipitationReference = 0f;

            var v = s.Validated();

            Assert.AreEqual(1f, v.IncomingAirMoisture);
            Assert.AreEqual(1f, v.GroundRainRecycling);
            Assert.AreEqual(0f, v.CapacityGrowth);
            Assert.AreEqual(1f, v.CloudFormationHumidity);
            Assert.AreEqual(0f, v.CloudRainRate);
            Assert.AreEqual(0f, v.AirAnticipationTiles);
            Assert.AreEqual(1f, v.AirLiftBeforeRidge);
            Assert.AreEqual(0f, v.AirRiseRate);
            Assert.AreEqual(1f, v.AirSinkRate);
            Assert.AreEqual(0, v.RainShadowSmoothing);
            Assert.AreEqual(PrecipitationSettings.MinPrecipitationReference, v.PrecipitationReference);
        }

        [Test]
        public void Validated_LeavesDefaultsUnchanged()
        {
            var d = PrecipitationSettings.Default;

            var v = d.Validated();

            Assert.AreEqual(d.WindFrom, v.WindFrom);
            Assert.AreEqual(d.IncomingAirMoisture, v.IncomingAirMoisture);
            Assert.AreEqual(d.GroundRainRecycling, v.GroundRainRecycling);
            Assert.AreEqual(d.CapacityGrowth, v.CapacityGrowth);
            Assert.AreEqual(d.CloudFormationHumidity, v.CloudFormationHumidity);
            Assert.AreEqual(d.CloudRainRate, v.CloudRainRate);
            Assert.AreEqual(d.AirAnticipationTiles, v.AirAnticipationTiles);
            Assert.AreEqual(d.AirLiftBeforeRidge, v.AirLiftBeforeRidge);
            Assert.AreEqual(d.AirRiseRate, v.AirRiseRate);
            Assert.AreEqual(d.AirSinkRate, v.AirSinkRate);
            Assert.AreEqual(d.RainShadowSmoothing, v.RainShadowSmoothing);
            Assert.AreEqual(d.PrecipitationReference, v.PrecipitationReference);
        }

        [Test]
        public void Validated_ReturnsACopy()
        {
            var s = PrecipitationSettings.Default;
            s.IncomingAirMoisture = 5f;

            s.Validated();

            Assert.AreEqual(5f, s.IncomingAirMoisture);
        }

        [Test]
        public void Default_WindComesFromTheWest()
        {
            Assert.AreEqual(CompassDirection.West, PrecipitationSettings.Default.WindFrom);
        }
    }
}
