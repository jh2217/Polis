using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>Debug visualization: renders vertex heights as a color-ramp texture. Colors are supplied by the caller.</summary>
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

        /// <summary>
        /// One pixel per vertex (so the texture is VertexSize, not Size), colored by height / maxHeight.
        /// Bilinear filtering blends between vertices, matching how tile surfaces are defined by their corners.
        /// </summary>
        public static Texture2D ToTexture(HeightMap map, int maxHeight, Color low, Color mid, Color high)
        {
            float inv = maxHeight > 0 ? 1f / maxHeight : 0f;
            var colors = new Color[map.Data.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Evaluate(map.Data[i] * inv, low, mid, high);

            var tex = new Texture2D(map.VertexSize.X, map.VertexSize.Y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
    }
}
