using Polis.Sim;
using UnityEngine;

namespace Polis.Unity
{
    /// <summary>Debug visualization: renders a vertex field as a color-ramp texture. Colors are supplied by the caller.</summary>
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

        /// <summary>Colors each vertex by height / maxHeight; see the FloatMap overload for the texture layout.</summary>
        public static Texture2D ToTexture(HeightMap map, int maxHeight, Color low, Color mid, Color high)
        {
            float inv = maxHeight > 0 ? 1f / maxHeight : 0f;
            var field = new FloatMap(map.Size);
            for (int i = 0; i < map.Data.Length; i++)
                field.Data[i] = map.Data[i] * inv;
            return ToTexture(field, low, mid, high);
        }

        /// <summary>Colors each vertex by where its value falls between min (low) and max (high); see the other FloatMap overload for the texture layout.</summary>
        public static Texture2D ToTexture(FloatMap field, float min, float max, Color low, Color mid, Color high)
        {
            float inv = max > min ? 1f / (max - min) : 0f;
            var normalized = new FloatMap(field.Size);
            for (int i = 0; i < field.Data.Length; i++)
                normalized.Data[i] = (field.Data[i] - min) * inv;
            return ToTexture(normalized, low, mid, high);
        }

        /// <summary>
        /// One pixel per vertex (so the texture is VertexSize, not Size), colored by the field value in [0, 1].
        /// Bilinear filtering blends between vertices, matching how tile surfaces are defined by their corners.
        /// </summary>
        public static Texture2D ToTexture(FloatMap field, Color low, Color mid, Color high)
        {
            var colors = new Color[field.Data.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Evaluate(field.Data[i], low, mid, high);

            var tex = new Texture2D(field.VertexSize.X, field.VertexSize.Y, TextureFormat.RGBA32, false)
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
