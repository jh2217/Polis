using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>Debug visualization: renders a float field in [0, 1] as a color-ramp texture. Colors are supplied by the caller.</summary>
    public static class Heatmap
    {
        /// <summary>Maps t (clamped to [0, 1]) onto a low → mid → high ramp.</summary>
        public static Color Evaluate(float t, Color low, Color mid, Color high)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f
                ? Color.Lerp(low, mid, t * 2f)
                : Color.Lerp(mid, high, (t - 0.5f) * 2f);
        }

        public static Texture2D ToTexture(HeightMap map, Color low, Color mid, Color high)
        {
            var colors = new Color[map.Data.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Evaluate(map.Data[i], low, mid, high);

            var tex = new Texture2D(map.Size.X, map.Size.Y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
    }
}
