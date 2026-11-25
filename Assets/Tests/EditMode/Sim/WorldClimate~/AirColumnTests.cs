using NUnit.Framework;

namespace Polis.Sim.Tests
{
    public class AirColumnTests
    {
        private static readonly float[] Onsets = { 0f, 0.3f, 0.66f, 0.8f, 0.95f, 1f };

        [Test]
        public void CloudWaterOf_IsZeroUpToTheOnsetHumidity()
        {
            foreach (float onset in Onsets)
            foreach (float humidity in new[] { 0f, onset * 0.5f, onset })
                Assert.AreEqual(0f, AirColumn.CloudWaterOf(humidity * 2f, 2f, onset), $"onset {onset}, humidity {humidity}");
        }

        [Test]
        public void CloudWaterOf_RisesWithoutAJumpPastTheOnsetHumidity()
        {
            foreach (float onset in new[] { 0.3f, 0.66f, 0.8f, 0.95f })
            {
                float justAbove = AirColumn.CloudWaterOf(onset + 0.001f, 1f, onset);

                Assert.Greater(justAbove, 0f, $"onset {onset}");
                Assert.Less(justAbove, 0.001f, $"onset {onset}");
            }
        }

        [Test]
        public void CloudWaterOf_GrowsWithTheTotalWater()
        {
            foreach (float onset in Onsets)
            {
                float previous = 0f;
                for (float total = 0f; total <= 5f; total += 0.01f)
                {
                    float cloud = AirColumn.CloudWaterOf(total, 1f, onset);

                    Assert.GreaterOrEqual(cloud, previous - 1e-6f, $"onset {onset}, total {total}");
                    previous = cloud;
                }
            }
        }

        [Test]
        public void CloudWaterOf_NeverExceedsTheTotalWater_AndIsNeverNegativeOrNotANumber()
        {
            foreach (float onset in Onsets)
            foreach (float total in new[] { 0f, 0.001f, 0.5f, 1f, 3f, 1000f })
            foreach (float capacity in new[] { 1e-9f, 0.01f, 1f, 50f })
            {
                float cloud = AirColumn.CloudWaterOf(total, capacity, onset);

                Assert.IsFalse(float.IsNaN(cloud) || float.IsInfinity(cloud), $"onset {onset}, total {total}, capacity {capacity}");
                Assert.That(cloud, Is.InRange(0f, total), $"onset {onset}, total {total}, capacity {capacity}");
            }
        }

        [Test]
        public void CloudWaterOf_ScalesWithTheUnitsTheWaterIsMeasuredIn()
        {
            float cloud = AirColumn.CloudWaterOf(0.9f, 1f, 0.8f);

            Assert.AreEqual(cloud * 7f, AirColumn.CloudWaterOf(0.9f * 7f, 1f * 7f, 0.8f), 1e-5f);
        }

        [Test]
        public void CloudWaterOf_AgreesWithAddingUpTheLayerInThinSlices()
        {
            // Water spread evenly through the layer, capacity falling by the factor 1 / onset from bottom to top:
            // the cloud at each height is what the water exceeds the capacity by there.
            foreach (float onset in new[] { 0.3f, 0.66f, 0.8f, 0.95f })
            foreach (float humidity in new[] { 0.5f, 0.85f, 1f, 1.5f, 4f })
            {
                float fall = -(float)System.Math.Log(onset);
                const int slices = 20000;
                double sum = 0;
                for (int i = 0; i < slices; i++)
                {
                    double capacityHere = System.Math.Exp(-fall * (i + 0.5) / slices);
                    sum += System.Math.Max(0.0, humidity - capacityHere);
                }

                Assert.AreEqual(sum / slices, AirColumn.CloudWaterOf(humidity, 1f, onset), 1e-4f, $"onset {onset}, humidity {humidity}");
            }
        }

        [Test]
        public void CloudWaterOf_WithAnOnsetOfOne_IsWhatExceedsTheCapacity()
        {
            Assert.AreEqual(0.5f, AirColumn.CloudWaterOf(1.5f, 1f, 1f), 1e-6f);
            Assert.AreEqual(0f, AirColumn.CloudWaterOf(0.9f, 1f, 1f));
        }

        [Test]
        public void CloudWaterOf_IsAboutATenthOfTheCapacityWhenTheBottomIsSaturated_AtAnOnsetOfEightyPercent()
        {
            // Known value: 1 - (1 - 0.8) / -ln(0.8).
            Assert.AreEqual(0.1037f, AirColumn.CloudWaterOf(1f, 1f, 0.8f), 1e-3f);
        }

        [Test]
        public void CapacityAt_IsOneAtTheReferenceTemperature_AndGrowsWithTemperature()
        {
            Assert.AreEqual(1f, AirColumn.CapacityAt(AirColumn.ReferenceTemperature, 0.06f), 1e-6f);

            float previous = 0f;
            for (float temperature = -40f; temperature <= 40f; temperature += 5f)
            {
                float capacity = AirColumn.CapacityAt(temperature, 0.06f);
                Assert.Greater(capacity, previous, $"{temperature} degrees C");
                previous = capacity;
            }
        }

        [Test]
        public void Entering_HoldsTheHumidityShareOfWhatTheLiftedAirCanHold()
        {
            var column = AirColumn.Entering(3f, 0.5f, 0.8f);

            Assert.AreEqual(0.4f, column.Water, 1e-6f);
            Assert.AreEqual(3f, column.LiftedHeight);
        }

        [Test]
        public void LiftedTo_KeepsTheWater()
        {
            var column = new AirColumn(0.7f, 2f).LiftedTo(9f);

            Assert.AreEqual(0.7f, column.Water);
            Assert.AreEqual(9f, column.LiftedHeight);
        }

        [Test]
        public void Recycled_AddsTheRecycledWaterWhenThereIsRoom()
        {
            Assert.AreEqual(0.5f, new AirColumn(0.2f, 0f).Recycled(1f, 0.3f).Water, 1e-6f);
        }

        [Test]
        public void Recycled_NeverPassesGroundSaturation()
        {
            Assert.AreEqual(1f, new AirColumn(0.8f, 0f).Recycled(1f, 0.5f).Water, 1e-6f);
        }

        [Test]
        public void Recycled_AddsNothingToAirSaturatedAtGroundLevel()
        {
            Assert.AreEqual(1.5f, new AirColumn(1.5f, 0f).Recycled(1f, 0.5f).Water);
        }

        [Test]
        public void Rained_ConservesWater_AndKeepsTheLiftedHeight()
        {
            var column = new AirColumn(1.2f, 2f);

            var after = column.Rained(1f, 0.8f, 0.3f, out float fallen);

            Assert.Greater(fallen, 0f);
            Assert.AreEqual(column.Water, after.Water + fallen, 1e-6f);
            Assert.AreEqual(2f, after.LiftedHeight);
        }

        [Test]
        public void Rained_NothingFallsBelowTheOnsetHumidity()
        {
            var after = new AirColumn(0.5f, 0f).Rained(1f, 0.8f, 1f, out float fallen);

            Assert.AreEqual(0f, fallen);
            Assert.AreEqual(0.5f, after.Water);
        }

        [Test]
        public void SurfaceHumidity_IsHigherOverColderGround_AndAtMostOne()
        {
            var column = new AirColumn(0.5f, 0f);

            Assert.Greater(column.SurfaceHumidity(0.6f), column.SurfaceHumidity(2f));
            Assert.AreEqual(1f, column.SurfaceHumidity(0.1f));
        }
    }
}
