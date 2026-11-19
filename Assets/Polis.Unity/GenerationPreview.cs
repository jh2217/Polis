using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Debug shell: generates an elevation map from the inspector settings and shows it as a heatmap-colored mesh.
    /// Add to any GameObject in a scene; right-click the component and choose "Regenerate" to reroll.
    /// </summary>
    public sealed class GenerationPreview : MonoBehaviour
    {
        // Vector2Int because Unity can't serialize the readonly MapSize; converted on use.
        [SerializeField] private Vector2Int mapSize = new Vector2Int(128, 128);
        [SerializeField] private ElevationGenerator.Settings settings = SeededDefault();
        [SerializeField] private Color lowColor = new Color(0.05f, 0.1f, 0.4f);
        [SerializeField] private Color midColor = new Color(0.2f, 0.65f, 0.25f);
        [SerializeField] private Color highColor = Color.white;

        private MeshFilter _meshFilter;
        private Material _material;

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

            _meshFilter.sharedMesh = TerrainMeshBuilder.Build(map);
            _material.mainTexture = Heatmap.ToTexture(map, Mathf.Max(1, settings.MaxHeight), lowColor, midColor, highColor);
            FrameCamera();
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

        private void FrameCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float extent = Mathf.Max(mapSize.x, mapSize.y);
            var center = transform.position + new Vector3(mapSize.x * 0.5f, 0f, mapSize.y * 0.5f);
            var rotation = Quaternion.Euler(45f, 0f, 0f);

            cam.orthographic = true;
            cam.orthographicSize = extent * 0.5f;
            cam.farClipPlane = extent * 4f;
            cam.transform.SetPositionAndRotation(center + rotation * new Vector3(0f, 0f, -extent * 2f), rotation);
        }
    }
}
