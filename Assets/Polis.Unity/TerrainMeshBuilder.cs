using System;
using System.Collections.Generic;
using Polis.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Polis.Unity
{
    /// <summary>
    /// Builds the terrain mesh from vertex heights. Each tile's top is the surface of its four corners
    /// (see TileShape); where a tile and its neighbor disagree about the height of a shared edge, a vertical
    /// cliff wall joins them. 1 height unit = TileShape.HeightUnitInTileWidths world units; tiles are 1 unit wide.
    /// </summary>
    public static class TerrainMeshBuilder
    {
        public static Mesh Build(HeightMap map)
        {
            int tw = map.Size.X;
            int th = map.Size.Y;

            var shapes = new TileShape[tw * th];
            for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
                shapes[ty * tw + tx] = TileShape.Of(map, tx, ty);

            var b = new Builder(map.VertexSize);
            for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                var s = shapes[ty * tw + tx];
                b.AddTop(tx, ty, s);

                // Walls on the +x and +y edges only, so each shared edge is handled once.
                if (tx + 1 < tw)
                {
                    var n = shapes[ty * tw + tx + 1];
                    b.AddWall(tx + 1, ty, tx + 1, ty + 1, s.H10, s.H11, n.H00, n.H01, Vector3.right);
                }
                if (ty + 1 < th)
                {
                    var n = shapes[(ty + 1) * tw + tx];
                    b.AddWall(tx, ty + 1, tx + 1, ty + 1, s.H01, s.H11, n.H00, n.H10, Vector3.forward);
                }
            }
            return b.ToMesh();
        }

        private sealed class Builder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();
            private readonly MapSize _vertexSize;

            public Builder(MapSize vertexSize) => _vertexSize = vertexSize;

            public void AddTop(int tx, int ty, TileShape s)
            {
                // Split along the diagonal with the smaller height difference to avoid needless creases.
                bool diagonal00To11 = Math.Abs(s.H00 - s.H11) <= Math.Abs(s.H10 - s.H01);
                AddQuad(Vector3.up, diagonal00To11,
                    Vertex(tx, ty, s.H00), Vertex(tx, ty + 1, s.H01),
                    Vertex(tx + 1, ty + 1, s.H11), Vertex(tx + 1, ty, s.H10));
            }

            /// <summary>
            /// Wall along the edge between vertices (vx0, vy0) and (vx1, vy1). a* are the heights the tile
            /// behind the edge assigns to its ends, b* the heights the neighbor assigns; towardNeighbor is
            /// the direction from the first tile to the second. The wall faces whichever side is lower.
            /// </summary>
            public void AddWall(int vx0, int vy0, int vx1, int vy1,
                int a0, int a1, int b0, int b1, Vector3 towardNeighbor)
            {
                if (a0 == b0 && a1 == b1) return;

                var lo0 = Vertex(vx0, vy0, Math.Min(a0, b0));
                var hi0 = Vertex(vx0, vy0, Math.Max(a0, b0));
                var lo1 = Vertex(vx1, vy1, Math.Min(a1, b1));
                var hi1 = Vertex(vx1, vy1, Math.Max(a1, b1));

                bool firstHigher = a0 >= b0 && a1 >= b1;
                bool neighborHigher = a0 <= b0 && a1 <= b1;
                if (firstHigher || !neighborHigher) AddQuad(towardNeighbor, true, lo0, hi0, hi1, lo1);
                // Heights cross along the edge: visible from both sides, so build both faces.
                if (neighborHigher || !firstHigher) AddQuad(-towardNeighbor, true, lo0, hi0, hi1, lo1);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh { name = "Terrain" };
                if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            private (Vector3 position, Vector2 uv) Vertex(int vx, int vy, int height) => (
                new Vector3(vx, height * TileShape.HeightUnitInTileWidths, vy),
                // Center of this vertex's pixel in the heatmap texture, which has one pixel per vertex.
                new Vector2((vx + 0.5f) / _vertexSize.X, (vy + 0.5f) / _vertexSize.Y));

            /// <summary>Adds a quad given in perimeter order, as two triangles wound to face `facing`.</summary>
            private void AddQuad(Vector3 facing, bool diagonal02,
                (Vector3 position, Vector2 uv) q0, (Vector3 position, Vector2 uv) q1,
                (Vector3 position, Vector2 uv) q2, (Vector3 position, Vector2 uv) q3)
            {
                int i = _vertices.Count;
                foreach (var q in new[] { q0, q1, q2, q3 })
                {
                    _vertices.Add(q.position);
                    _uvs.Add(q.uv);
                }

                if (diagonal02)
                {
                    AddTriangle(facing, i, i + 1, i + 2);
                    AddTriangle(facing, i, i + 2, i + 3);
                }
                else
                {
                    AddTriangle(facing, i, i + 1, i + 3);
                    AddTriangle(facing, i + 1, i + 2, i + 3);
                }
            }

            private void AddTriangle(Vector3 facing, int a, int b, int c)
            {
                var normal = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]);
                if (Vector3.Dot(normal, facing) < 0f) (b, c) = (c, b);
                _triangles.Add(a);
                _triangles.Add(b);
                _triangles.Add(c);
            }
        }
    }
}
