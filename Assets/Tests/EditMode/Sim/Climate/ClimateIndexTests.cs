using NUnit.Framework;
using Polis.Sim.Climate;
using Polis.Sim.Grids;

namespace Polis.Sim.Tests.Climate
{
    public class ClimateIndexTests
    {
        private static FloatMap FieldOf(params float[] values)
        {
            // A 1-tile-high strip has 2 rows of vertices; fill both rows with the values.
            var field = new FloatMap(values.Length - 1, 1);
            for (int x = 0; x < values.Length; x++)
            {
                field[x, 0] = values[x];
                field[x, 1] = values[x];
            }
            return field;
        }

        [Test]
        public void TemperatureIndexOf_IsZeroAtTheColdestAndOneAtTheHottestTemperature()
        {
            var index = ClimateIndex.TemperatureIndexOf(FieldOf(ClimateIndex.ColdestTemperature, ClimateIndex.HottestTemperature));

            Assert.AreEqual(0f, index[0, 0], 1e-6f);
            Assert.AreEqual(1f, index[1, 0], 1e-6f);
        }

        [Test]
        public void TemperatureIndexOf_HoldsTemperaturesBeyondTheRangeAtTheEnds()
        {
            var index = ClimateIndex.TemperatureIndexOf(FieldOf(ClimateIndex.ColdestTemperature - 40f, ClimateIndex.HottestTemperature + 40f));

            Assert.AreEqual(0f, index[0, 0]);
            Assert.AreEqual(1f, index[1, 0]);
        }

        [Test]
        public void TemperatureIndexOf_RisesWithTemperature()
        {
            var index = ClimateIndex.TemperatureIndexOf(FieldOf(0f, 10f, 20f));

            Assert.Less(index[0, 0], index[1, 0]);
            Assert.Less(index[1, 0], index[2, 0]);
        }

        [Test]
        public void PrecipitationIndexOf_IsZeroWithNoRain()
        {
            Assert.AreEqual(0f, ClimateIndex.PrecipitationIndexOf(FieldOf(0f, 0f))[0, 0]);
        }

        [Test]
        public void PrecipitationIndexOf_IsAboutSixtyThreePercentAtTheReference()
        {
            var index = ClimateIndex.PrecipitationIndexOf(FieldOf(ClimateIndex.PrecipitationReference, 0f));

            Assert.AreEqual(0.632f, index[0, 0], 1e-3f);
        }

        [Test]
        public void PrecipitationIndexOf_RisesWithRainAndStaysBelowOne()
        {
            var index = ClimateIndex.PrecipitationIndexOf(FieldOf(250f, 1000f, 3000f));

            Assert.Less(index[0, 0], index[1, 0]);
            Assert.Less(index[1, 0], index[2, 0]);
            Assert.Less(index[2, 0], 1f);
        }
    }
}
