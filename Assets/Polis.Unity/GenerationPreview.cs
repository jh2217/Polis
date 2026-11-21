using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Debug shell: generates a map from the inspector settings and hands it to the MapHost on the same GameObject
    /// (add a TerrainView next to it to see the result).
    /// Right-click the component and choose "Regenerate" to reroll.
    /// </summary>
    [RequireComponent(typeof(MapHost))]
    public sealed class GenerationPreview : MonoBehaviour
    {
        // Vector2Int because Unity can't serialize the readonly MapSize; converted on use.
        [SerializeField] private Vector2Int mapSize = new Vector2Int(128, 128);
        [SerializeField] private MapGenerator.Settings settings = SeededDefault();

        private void Start() => Regenerate();

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            // With UseSeed off, a time-based RNG makes each Regenerate give a new map.
            var rng = new SimRng((ulong)System.Environment.TickCount);
            var state = MapGenerator.Generate(new MapSize(mapSize.x, mapSize.y), rng, settings);
            GetComponent<MapHost>().Set(state);
        }

        // Field initializer for new components; existing ones keep their serialized values.
        private static MapGenerator.Settings SeededDefault()
        {
            var s = MapGenerator.Settings.Default;
            s.Elevation.UseSeed = true;
            s.Elevation.Seed = 1;
            return s;
        }
    }
}
