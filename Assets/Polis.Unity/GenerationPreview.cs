using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Debug shell: generates an elevation map from the inspector settings and shows it as a heatmap.
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

        private const float PixelsPerUnit = 16f;

        private SpriteRenderer _renderer;

        private void Start() => Regenerate();

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            // With UseSeed off, a time-based RNG makes each Regenerate give a new map.
            var rng = new SimRng((ulong)System.Environment.TickCount);
            var map = ElevationGenerator.Generate(new MapSize(mapSize.x, mapSize.y), rng, settings);

            if (_renderer == null)
                _renderer = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();

            var tex = Heatmap.ToTexture(map, lowColor, midColor, highColor);
            _renderer.sprite = Sprite.Create(tex, new Rect(0, 0, mapSize.x, mapSize.y), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            FrameCamera();
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
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(mapSize.x, mapSize.y) / PixelsPerUnit * 0.5f;
            cam.transform.SetPositionAndRotation(
                transform.position + new Vector3(0f, 0f, -10f), Quaternion.identity);
        }
    }
}
