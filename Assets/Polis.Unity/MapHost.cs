using System;
using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>
    /// Holds the current map and announces when it changes. Its transform is the map's world origin.
    /// A producer calls Set; consumers read Current when they enable and subscribe to Changed for later maps.
    /// </summary>
    public sealed class MapHost : MonoBehaviour
    {
        /// <summary>The current map, or null before the first one is set.</summary>
        public MapState Current { get; private set; }

        /// <summary>Raised after Current is replaced.</summary>
        public event Action<MapState> Changed;

        public void Set(MapState state)
        {
            Current = state;
            Changed?.Invoke(state);
        }

        /// <summary>World-space bounds of a map placed at this host's origin, with heights scaled to world units.</summary>
        public Bounds BoundsOf(MapState state)
        {
            var size = new Vector3(state.Size.X, Mathf.Max(1, state.MaxHeight) * TileShape.HeightUnitInTileWidths, state.Size.Y);
            return new Bounds(transform.position + size * 0.5f, size);
        }
    }
}
