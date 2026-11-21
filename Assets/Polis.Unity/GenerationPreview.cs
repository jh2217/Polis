using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Debug shell: generates an elevation map from the inspector settings and shows it as a heatmap-colored mesh.
    /// Add to any GameObject in a scene (a MapHost is added alongside it and receives each generated map);
    /// right-click the component and choose "Regenerate" to reroll.
    /// </summary>
    [RequireComponent(typeof(MapHost))]
    public sealed class GenerationPreview : MonoBehaviour
    {
        // Vector2Int because Unity can't serialize the readonly MapSize; converted on use.
        [SerializeField] private Vector2Int mapSize = new Vector2Int(128, 128);
        [SerializeField] private ElevationGenerator.Settings settings = SeededDefault();
        [SerializeField] private Color lowColor = new Color(0.05f, 0.1f, 0.4f);
        [SerializeField] private Color midColor = new Color(0.2f, 0.65f, 0.25f);
        [SerializeField] private Color highColor = Color.white;
        [Tooltip("Outline every tile on the terrain. Toggle at runtime from the inspector.")]
        [SerializeField] private bool showGrid = true;

        private MeshFilter _meshFilter;
        private Material _material;
        private MeshFilter _gridFilter;
        private Material _gridMaterial;

        // Fires when the inspector changes, including in play mode, so the checkbox toggles the grid live.
        private void OnValidate()
        {
            ApplyGridSettings();
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
            var map = ElevationGenerator.Generate(new MapSize(mapSize.x, mapSize.y), rng, settings);

            EnsureTerrainObject();

            // Free the previous mesh/texture so repeated Regenerate calls don't leak.
            if (_meshFilter.sharedMesh != null) DestroyObject(_meshFilter.sharedMesh);
            if (_material.mainTexture != null) DestroyObject(_material.mainTexture);
            if (_gridFilter.sharedMesh != null) DestroyObject(_gridFilter.sharedMesh);

            _gridFilter.sharedMesh = TerrainGridBuilder.Build(map);
            ApplyGridSettings();
            _meshFilter.sharedMesh = TerrainMeshBuilder.Build(map);
            _material.mainTexture = Heatmap.ToTexture(map, Mathf.Max(1, settings.MaxHeight), lowColor, midColor, highColor);

            GetComponent<MapHost>().Set(new MapState(map));
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
