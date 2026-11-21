using System.Collections.Generic;
using Polis.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Polis.Unity
{
    /// <summary>
    /// Builds a line mesh outlining every tile on the terrain surface, following each tile's top edges.
    /// Edges shared by tiles at the same height are drawn once; where a cliff separates them, each side
    /// is drawn at its own height, which outlines the top and bottom of the wall. Vertical lines at tile corners
    /// outline the wall's sides.
    /// </summary>
    public static class TerrainGridBuilder
    {
        /// <summary>Lift above the surface so lines don't z-fight with the terrain.</summary>
        private const float Lift = 0.02f;

        public static Mesh Build(HeightMap map)
        {
            var b = new Builder(map);
            b.AddTileEdges();
            b.AddCliffSides();
            return b.ToMesh();
        }

        private sealed class Builder
        {
            private readonly int _tw;
            private readonly int _th;
            private readonly TileShape[] _shapes;
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<int> _indices = new List<int>();

            public Builder(HeightMap map)
            {
                _tw = map.Size.X;
                _th = map.Size.Y;
                _shapes = new TileShape[_tw * _th];
                for (int ty = 0; ty < _th; ty++)
                for (int tx = 0; tx < _tw; tx++)
                    _shapes[ty * _tw + tx] = TileShape.Of(map, tx, ty);
            }

            /// <summary>
            /// Each tile draws its bottom and left edges; right and top edges only on the map border or where
            /// the neighbor disagrees about the edge height (otherwise the neighbor already drew it).
            /// </summary>
            public void AddTileEdges()
            {
                for (int ty = 0; ty < _th; ty++)
                for (int tx = 0; tx < _tw; tx++)
                {
                    var s = Shape(tx, ty);
                    AddLine(tx, ty, s.H00, tx + 1, ty, s.H10);
                    AddLine(tx, ty, s.H00, tx, ty + 1, s.H01);

                    if (tx + 1 == _tw || !SameRightEdge(s, Shape(tx + 1, ty)))
                        AddLine(tx + 1, ty, s.H10, tx + 1, ty + 1, s.H11);

                    if (ty + 1 == _th || !SameTopEdge(s, Shape(tx, ty + 1)))
                        AddLine(tx, ty + 1, s.H01, tx + 1, ty + 1, s.H11);
                }
            }

            /// <summary>
            /// Cliff walls end in vertical edges at tile corners. At each vertex, one line spans the lowest to the
            /// highest height its surrounding tiles assign it; that covers every wall edge meeting there.
            /// </summary>
            public void AddCliffSides()
            {
                for (int vy = 0; vy <= _th; vy++)
                for (int vx = 0; vx <= _tw; vx++)
                {
                    var (low, high) = HeightRangeAt(vx, vy);
                    if (low < high) AddLine(vx, vy, low, vx, vy, high);
                }
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh { name = "TerrainGrid" };
                if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetIndices(_indices, MeshTopology.Lines, 0);
                mesh.RecalculateBounds();
                return mesh;
            }

            private TileShape Shape(int tx, int ty) => _shapes[ty * _tw + tx];

            // A tile's right edge is the neighbor's left edge, and its top edge is the neighbor's bottom edge.
            private static bool SameRightEdge(TileShape s, TileShape right) => right.H00 == s.H10 && right.H01 == s.H11;

            private static bool SameTopEdge(TileShape s, TileShape above) => above.H00 == s.H01 && above.H10 == s.H11;

            /// <summary>The lowest and highest height that the tiles around vertex (vx, vy) assign to it.</summary>
            private (int low, int high) HeightRangeAt(int vx, int vy)
            {
                int low = int.MaxValue;
                int high = int.MinValue;

                void Include(int h)
                {
                    if (h < low) low = h;
                    if (h > high) high = h;
                }

                // The vertex is a different corner of each of the four tiles around it.
                if (vx < _tw && vy < _th) Include(Shape(vx, vy).H00);
                if (vx > 0 && vy < _th) Include(Shape(vx - 1, vy).H10);
                if (vx < _tw && vy > 0) Include(Shape(vx, vy - 1).H01);
                if (vx > 0 && vy > 0) Include(Shape(vx - 1, vy - 1).H11);
                return (low, high);
            }

            private void AddLine(float x0, float z0, int h0, float x1, float z1, int h1)
            {
                _indices.Add(_vertices.Count);
                _indices.Add(_vertices.Count + 1);
                _vertices.Add(new Vector3(x0, h0 * TileShape.HeightUnitInTileWidths + Lift, z0));
                _vertices.Add(new Vector3(x1, h1 * TileShape.HeightUnitInTileWidths + Lift, z1));
            }
        }
    }
}
