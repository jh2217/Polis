using NUnit.Framework;
using Polis.Unity;
using Polis.Unity.CameraControl;
using UnityEngine;

namespace Polis.Unity.Tests.CameraControl
{
    public class CameraRigTests
    {
        [Test]
        public void ZoomedSize_PositiveNotches_ZoomsIn()
        {
            Assert.Less(CameraRig.ZoomedSize(50f, 1f, 0.15f, 1f, 100f), 50f);
        }

        [Test]
        public void ZoomedSize_NegativeNotches_ZoomsOut()
        {
            Assert.Greater(CameraRig.ZoomedSize(50f, -1f, 0.15f, 1f, 100f), 50f);
        }

        [Test]
        public void ZoomedSize_OppositeNotches_Cancel()
        {
            float there = CameraRig.ZoomedSize(50f, 3f, 0.15f, 1f, 100f);
            Assert.AreEqual(50f, CameraRig.ZoomedSize(there, -3f, 0.15f, 1f, 100f), 1e-3f);
        }

        [Test]
        public void ZoomedSize_ClampsToLimits()
        {
            Assert.AreEqual(4f, CameraRig.ZoomedSize(50f, 100f, 0.15f, 4f, 100f), 1e-4f);
            Assert.AreEqual(100f, CameraRig.ZoomedSize(50f, -100f, 0.15f, 4f, 100f), 1e-4f);
        }

        [Test]
        public void ClampFocus_KeepsGroundPositionInsideBounds_AndLeavesHeight()
        {
            var bounds = new Bounds(new Vector3(5f, 1f, 5f), new Vector3(10f, 2f, 10f));

            var clamped = CameraRig.ClampFocus(new Vector3(-3f, 7f, 40f), bounds);

            Assert.AreEqual(new Vector3(0f, 7f, 10f), clamped);
        }

        [Test]
        public void RotatedDirection_WrapsInBothDirections()
        {
            Assert.AreEqual(0, CameraRig.RotatedDirection(CameraRig.ViewDirections - 1, 1));
            Assert.AreEqual(CameraRig.ViewDirections - 1, CameraRig.RotatedDirection(0, -1));
        }

        [Test]
        public void RotatedDirection_FullTurn_ReturnsToStart()
        {
            Assert.AreEqual(3, CameraRig.RotatedDirection(3, CameraRig.ViewDirections));
        }

        [Test]
        public void YawOf_StepsAreFortyFiveDegrees()
        {
            Assert.AreEqual(0f, CameraRig.YawOf(0));
            Assert.AreEqual(45f, CameraRig.YawOf(1), 1e-4f);
            Assert.AreEqual(315f, CameraRig.YawOf(7), 1e-4f);
        }

        [Test]
        public void ClampFocus_InsideBounds_IsUnchanged()
        {
            var bounds = new Bounds(new Vector3(5f, 1f, 5f), new Vector3(10f, 2f, 10f));
            var focus = new Vector3(3f, 1f, 8f);

            Assert.AreEqual(focus, CameraRig.ClampFocus(focus, bounds));
        }
    }
}
