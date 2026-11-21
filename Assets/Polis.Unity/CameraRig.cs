using Polis.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Polis.Unity
{
    /// <summary>
    /// Orthographic pan/zoom/rotate camera at a fixed pitch. Buttons come from <see cref="CameraControls"/>.
    /// Rotation snaps to eight view directions 45 degrees apart, alternating between axis-aligned and diagonal views.
    /// The focus stays inside the framed bounds. Call Frame to point it at a map.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        public const int ViewDirections = 8;

        [SerializeField] private CameraControls controls = new CameraControls();
        [SerializeField, Range(10f, 80f)] private float pitch = 45f;
        [Tooltip("Zoom change per scroll notch: orthographic size is multiplied by e^(-notches * this).")]
        [SerializeField] private float zoomStep = 0.15f;
        [Tooltip("Closest zoom, as orthographic size in world units (half the visible height).")]
        [SerializeField] private float minOrthoSize = 4f;
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float rotateSpeed = 360f;

        [Tooltip("Frames the camera on the map each time this host gets a new one.")]
        [SerializeField] private MapHost mapHost;

        private Camera _camera;
        private bool _framed;
        private Bounds _bounds;
        private Vector3 _focus;
        private float _distance = 100f;
        private float _maxOrthoSize = 100f;
        private int _direction;
        private float _yaw;

        /// <summary>Zooms by `notches` scroll steps (positive zooms in), clamped to [min, max].</summary>
        public static float ZoomedSize(float size, float notches, float zoomStep, float min, float max) =>
            Mathf.Clamp(size * Mathf.Exp(-notches * zoomStep), min, max);

        /// <summary>Clamps the focus point's ground position (x and z) to the bounds; height is unchanged.</summary>
        public static Vector3 ClampFocus(Vector3 focus, Bounds bounds) => new Vector3(
            Mathf.Clamp(focus.x, bounds.min.x, bounds.max.x),
            focus.y,
            Mathf.Clamp(focus.z, bounds.min.z, bounds.max.z));

        /// <summary>The view direction after `steps` quarter-octant turns (positive turns right), wrapped to [0, ViewDirections).</summary>
        public static int RotatedDirection(int direction, int steps) =>
            ((direction + steps) % ViewDirections + ViewDirections) % ViewDirections;

        /// <summary>Camera yaw in degrees for a view direction; each step is 45 degrees.</summary>
        public static float YawOf(int direction) => direction * (360f / ViewDirections);

        /// <summary>Centers the camera on the bounds, zoomed out far enough to see all of it, and limits panning to it.</summary>
        public void Frame(Bounds bounds)
        {
            _framed = true;
            _bounds = bounds;
            _focus = bounds.center;

            float extent = Mathf.Max(bounds.size.x, bounds.size.z);
            _maxOrthoSize = extent * 0.6f;
            _distance = extent * 2f;

            _camera.orthographic = true;
            _camera.orthographicSize = _maxOrthoSize;
            _camera.farClipPlane = _distance * 2f;
            _yaw = YawOf(_direction);
            Apply();
        }

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable()
        {
            if (mapHost == null) return;
            mapHost.Changed += OnMapChanged;
            if (mapHost.Current != null) OnMapChanged(mapHost.Current);
        }

        private void OnDisable()
        {
            if (mapHost != null) mapHost.Changed -= OnMapChanged;
        }

        private void OnMapChanged(MapState state) => Frame(mapHost.BoundsOf(state));

        private void Update()
        {
            if (!_framed) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            float size = _camera.orthographicSize;

            if (controls.RotateLeftPressed(keyboard, mouse)) _direction = RotatedDirection(_direction, -1);
            if (controls.RotateRightPressed(keyboard, mouse)) _direction = RotatedDirection(_direction, 1);
            _yaw = Mathf.MoveTowardsAngle(_yaw, YawOf(_direction), rotateSpeed * Time.deltaTime);

            float notches = controls.ZoomNotches(keyboard, mouse, Time.deltaTime);
            if (notches != 0f)
            {
                size = ZoomedSize(size, notches, zoomStep, Mathf.Min(minOrthoSize, _maxOrthoSize), _maxOrthoSize);
                _camera.orthographicSize = size;
            }

            if (controls.IsPanning(mouse))
            {
                Vector2 drag = mouse.delta.ReadValue();
                float worldPerPixel = 2f * size / Screen.height;
                // Ground distance up the screen is stretched by 1 / sin(pitch). Dragging moves the world with the
                // cursor, so the focus moves the opposite way.
                var screenMove = new Vector3(drag.x * worldPerPixel, 0f, drag.y * worldPerPixel / Mathf.Sin(pitch * Mathf.Deg2Rad));
                _focus -= Quaternion.Euler(0f, _yaw, 0f) * screenMove;
            }

            _focus = ClampFocus(_focus, _bounds);
            Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(pitch, _yaw, 0f);
            transform.SetPositionAndRotation(_focus + rotation * new Vector3(0f, 0f, -_distance), rotation);
        }
    }
}
