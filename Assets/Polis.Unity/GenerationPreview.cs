using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Debug shell: generates a map from the inspector settings and shows it as a mesh colored by a selectable field.
    /// Add to any GameObject in a scene (a MapHost is added alongside it and receives each generated map);
    /// right-click the component and choose "Regenerate" to reroll.
    /// </summary>
    [RequireComponent(typeof(MapHost))]
    public sealed class GenerationPreview : MonoBehaviour
    {
        // Vector2Int because Unity can't serialize the readonly MapSize; converted on use.
        [SerializeField] private Vector2Int mapSize = new Vector2Int(128, 128);
        [SerializeField] private ElevationGenerator.Settings settings = SeededDefault();
        [Tooltip("Which field colors the terrain. Changing it recolors the current map without regenerating.")]
        [SerializeField] private DebugField shownField = DebugField.Elevation;
        [SerializeField] private Color lowColor = new Color(0.05f, 0.1f, 0.4f);
        [SerializeField] private Color midColor = new Color(0.2f, 0.65f, 0.25f);
        [SerializeField] private Color highColor = Color.white;
        [Tooltip("Flatness colors: low = rough terrain, high = flat terrain.")]
        [SerializeField] private Color roughColor = new Color(0.3f, 0.22f, 0.2f);
        [SerializeField] private Color halfFlatColor = new Color(0.75f, 0.65f, 0.4f);
        [SerializeField] private Color flatColor = new Color(0.85f, 0.95f, 0.7f);
        [Tooltip("Outline every tile on the terrain. Toggle at runtime from the inspector.")]
        [SerializeField] private bool showGrid = true;

        public enum DebugField
        {
            Elevation,
            Flatness,
        }

        private MeshFilter _meshFilter;
        private Material _material;
        private MeshFilter _gridFilter;
        private Material _gridMaterial;
        private MapState _state;
        private int _colorMaxHeight = 1;

        // Fires when the inspector changes, including in play mode, so the checkbox and field selector act live.
        private void OnValidate()
        {
            ApplyGridSettings();
            ApplyFieldTexture();
        }

        private void ApplyFieldTexture()
        {
            if (_state == null || _material == null) return;

            if (_material.mainTexture != null) DestroyObject(_material.mainTexture);
            _material.mainTexture = shownField == DebugField.Flatness
                ? Heatmap.ToTexture(_state.Flatness, roughColor, halfFlatColor, flatColor)
                : Heatmap.ToTexture(_state.Heights, _colorMaxHeight, lowColor, midColor, highColor);
        }

        private void ApplyGridSettings()
        {
            if (_gridFilter == null) return;
            _gridFilter.gameObject.SetActive(showGrid);
        }

        private void Start() => Regenerate();

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            // With UseSeed off, a time-based RNG makes each Regenerate give a new map.
            var rng = new SimRng((ulong)System.Environment.TickCount);
            var generated = ElevationGenerator.Generate(new MapSize(mapSize.x, mapSize.y), rng, settings);
            _state = new MapState(generated.Heights, generated.Flatness);
            _colorMaxHeight = Mathf.Max(1, settings.MaxHeight);

            EnsureTerrainObject();

            // Free the previous mesh so repeated Regenerate calls don't leak.
            if (_meshFilter.sharedMesh != null) DestroyObject(_meshFilter.sharedMesh);
            if (_gridFilter.sharedMesh != null) DestroyObject(_gridFilter.sharedMesh);

            _gridFilter.sharedMesh = TerrainGridBuilder.Build(_state.Heights);
            ApplyGridSettings();
            _meshFilter.sharedMesh = TerrainMeshBuilder.Build(_state.Heights);
            ApplyFieldTexture();

            GetComponent<MapHost>().Set(_state);
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

        // Field initializer for new components; existing ones keep their serialized values.
        private static ElevationGenerator.Settings SeededDefault()
        {
            var s = ElevationGenerator.Settings.Default;
            s.UseSeed = true;
            s.Seed = 1;
            return s;
        }
    }
}
