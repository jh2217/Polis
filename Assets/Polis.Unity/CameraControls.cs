using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Polis.Unity
{
    /// <summary>The buttons that drive the camera and how fast held keys generate input, as serializable settings so they can be changed in the inspector or by the user.</summary>
    [Serializable]
    public sealed class CameraControls
    {
        public MouseButton PanButton = MouseButton.Middle;
        public Key RotateLeftKey = Key.LeftArrow;
        public Key RotateRightKey = Key.RightArrow;
        public MouseButton RotateLeftButton = MouseButton.Forward;
        public MouseButton RotateRightButton = MouseButton.Back; 
        public Key ZoomInKey = Key.UpArrow;
        public Key ZoomOutKey = Key.DownArrow;
        [Tooltip("Scroll-notch equivalents per second while a zoom key is held.")]
        public float KeyZoomNotchesPerSecond = 12f;

        public bool IsPanning(Mouse mouse) => Held(mouse, PanButton);

        public bool RotateLeftPressed(Keyboard keyboard, Mouse mouse) =>
            Pressed(keyboard, RotateLeftKey) || Pressed(mouse, RotateLeftButton);

        public bool RotateRightPressed(Keyboard keyboard, Mouse mouse) =>
            Pressed(keyboard, RotateRightKey) || Pressed(mouse, RotateRightButton);

        /// <summary>
        /// Zoom requested this frame, in scroll notches (positive zooms in): the zoom keys held for `deltaTime`
        /// seconds at KeyZoomNotchesPerSecond, plus one notch per scroll event in its direction. Raw scroll units
        /// differ by platform and device, so only the scroll direction is used.
        /// </summary>
        public float ZoomNotches(Keyboard keyboard, Mouse mouse, float deltaTime)
        {
            float notches = 0f;
            if (keyboard != null)
            {
                float axis = (keyboard[ZoomInKey].isPressed ? 1f : 0f) - (keyboard[ZoomOutKey].isPressed ? 1f : 0f);
                notches += axis * KeyZoomNotchesPerSecond * deltaTime;
            }
            if (mouse != null) notches += Math.Sign(mouse.scroll.ReadValue().y);
            return notches;
        }

        private static bool Pressed(Keyboard keyboard, Key key) => keyboard != null && keyboard[key].wasPressedThisFrame;

        private static bool Pressed(Mouse mouse, MouseButton button) => Button(mouse, button)?.wasPressedThisFrame ?? false;

        private static bool Held(Mouse mouse, MouseButton button) => Button(mouse, button)?.isPressed ?? false;

        private static ButtonControl Button(Mouse mouse, MouseButton button)
        {
            if (mouse == null) return null;
            switch (button)
            {
                case MouseButton.Left: return mouse.leftButton;
                case MouseButton.Right: return mouse.rightButton;
                case MouseButton.Middle: return mouse.middleButton;
                case MouseButton.Forward: return mouse.forwardButton;
                case MouseButton.Back: return mouse.backButton;
                default: return null;
            }
        }
    }
}
