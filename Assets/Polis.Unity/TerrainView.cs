using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Shows the MapHost's current map on the same GameObject: a terrain mesh colored by a selectable field,
    /// with an optional grid overlay. Rebuilds whenever the host gets a new map.
    /// </summary>
    [RequireComponent(typeof(MapHost))]
    public sealed class TerrainView : MonoBehaviour
    {
        [Tooltip("Which field colors the terrain. Changing it recolors the current map without regenerating.")]
        [SerializeField] private DebugField shownField = DebugField.Elevation;
        [Tooltip("Elevation colors: low = lowest ground, high = highest.")]
        [SerializeField] private Ramp elevationColors = new Ramp(new Color(0.05f, 0.1f, 0.4f), new Color(0.2f, 0.65f, 0.25f), Color.white);
        [Tooltip("Flatness colors: low = rough terrain, high = flat terrain.")]
        [SerializeField] private Ramp flatnessColors = new Ramp(new Color(0.3f, 0.22f, 0.2f), new Color(0.75f, 0.65f, 0.4f), new Color(0.85f, 0.95f, 0.7f));
        [Tooltip("Temperature colors: low = coldest of the range below, high = hottest.")]
        [SerializeField] private Ramp temperatureColors = new Ramp(new Color(0.25f, 0.4f, 0.9f), new Color(0.95f, 0.9f, 0.4f), new Color(0.85f, 0.2f, 0.1f));
        [Tooltip("Temperatures in degrees C shown as the lowest and highest temperature color.")]
        [SerializeField] private Vector2 temperatureRange = new Vector2(-10f, 30f);
        [Tooltip("Precipitation colors: low = dry, high = wet.")]
        [SerializeField] private Ramp precipitationColors = new Ramp(new Color(0.85f, 0.7f, 0.4f), new Color(0.4f, 0.7f, 0.35f), new Color(0.1f, 0.35f, 0.75f));
        [Tooltip("Precipitation in mm per year shown as the lowest and highest precipitation color.")]
        [SerializeField] private Vector2 precipitationRange = new Vector2(0f, 3000f);
        [Tooltip("Surface humidity colors: low = dry air at ground level, high = saturated (fog).")]
        [SerializeField] private Ramp surfaceHumidityColors = new Ramp(new Color(0.9f, 0.75f, 0.45f), new Color(0.55f, 0.8f, 0.7f), new Color(0.85f, 0.9f, 1f));
        [Tooltip("Outline every tile on the terrain. Toggle at runtime from the inspector.")]
        [SerializeField] private bool showGrid = true;

        /// <summary>The three colors a field's values are mapped onto, from lowest to highest.</summary>
        [System.Serializable]
        private struct Ramp
        {
            public Color Low;
            public Color Mid;
            public Color High;

            public Ramp(Color low, Color mid, Color high)
            {
                Low = low;
                Mid = mid;
                High = high;
            }
        }

        public enum DebugField
        {
            Elevation,
            Flatness,
            Temperature,
            Precipitation,
            SurfaceHumidity,
        }

        private MapHost _host;
        private MapState _state;
        private MeshFilter _meshFilter;
        private Material _material;
        private MeshFilter _gridFilter;
        private Material _gridMaterial;

        private void Awake() => _host = GetComponent<MapHost>();

        private void OnEnable()
        {
            _host.Changed += Rebuild;
            if (_host.Current != null) Rebuild(_host.Current);
        }

        private void OnDisable() => _host.Changed -= Rebuild;

        // Fires when the inspector changes, including in play mode, so the checkbox and field selector act live.
        private void OnValidate()
        {
            ApplyGridSettings();
            ApplyFieldTexture();
        }

        private void Rebuild(MapState state)
        {
            _state = state;
            EnsureTerrainObject();

            // Free the previous mesh so repeated rebuilds don't leak.
            if (_meshFilter.sharedMesh != null) DestroyObject(_meshFilter.sharedMesh);
            if (_gridFilter.sharedMesh != null) DestroyObject(_gridFilter.sharedMesh);

            _meshFilter.sharedMesh = TerrainMeshBuilder.Build(state.Heights);
            _gridFilter.sharedMesh = TerrainGridBuilder.Build(state.Heights);
            ApplyGridSettings();
            ApplyFieldTexture();
        }

        private void ApplyGridSettings()
        {
            if (_gridFilter == null) return;
            _gridFilter.gameObject.SetActive(showGrid);
        }

        private void ApplyFieldTexture()
        {
            if (_state == null || _material == null) return;

            if (_material.mainTexture != null) DestroyObject(_material.mainTexture);
            _material.mainTexture = ShownTexture();
        }

        private Texture2D ShownTexture()
        {
            switch (shownField)
            {
                case DebugField.Flatness:
                    return Heatmap.ToTexture(_state.Flatness, flatnessColors.Low, flatnessColors.Mid, flatnessColors.High);
                case DebugField.Temperature:
                    return Heatmap.ToTexture(_state.Temperature, temperatureRange.x, temperatureRange.y, temperatureColors.Low, temperatureColors.Mid, temperatureColors.High);
                case DebugField.Precipitation:
                    return Heatmap.ToTexture(_state.Precipitation, precipitationRange.x, precipitationRange.y, precipitationColors.Low, precipitationColors.Mid, precipitationColors.High);
                case DebugField.SurfaceHumidity:
                    return Heatmap.ToTexture(_state.SurfaceHumidity, 0f, 1f, surfaceHumidityColors.Low, surfaceHumidityColors.Mid, surfaceHumidityColors.High);
                default:
                    return Heatmap.ToTexture(_state.Heights, _state.MaxHeight, elevationColors.Low, elevationColors.Mid, elevationColors.High);
            }
        }

        private void EnsureTerrainObject()
        {
            if (_meshFilter != null) return;

            var go = new GameObject("Terrain");
            go.transform.SetParent(transform, false);
            _meshFilter = go.AddComponent<MeshFilter>();

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            _material = new Material(shader);
            go.AddComponent<MeshRenderer>().sharedMaterial = _material;

            var grid = new GameObject("Grid");
            grid.transform.SetParent(transform, false);
            _gridFilter = grid.AddComponent<MeshFilter>();
            // The shared material asset is used directly (not copied) so editing it changes the grid live.
            // The grid shader fades lines by on-screen tile size; see Resources/GridLines.shader.
            _gridMaterial = Resources.Load<Material>("GridLines") ?? new Material(Shader.Find("Polis/GridLines") ?? shader);
            grid.AddComponent<MeshRenderer>().sharedMaterial = _gridMaterial;
        }

        private static void DestroyObject(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
