using System.Collections.Generic;
using Polis.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Polis.Unity
{
    /// <summary>
    /// Builds a line mesh outlining every tile on the terrain surface, following each tile's top edges.
    /// Edges shared by tiles at the same height are drawn once; where a cliff separates them, each side
    /// is drawn at its own height, which outlines the top and bottom of the wall.
    /// </summary>
    public static class TerrainGridBuilder
    {
        /// <summary>Lift above the surface so lines don't z-fight with the terrain.</summary>
        private const float Lift = 0.02f;

        public static Mesh Build(HeightMap map)
        {
            int tw = map.Size.X;
            int th = map.Size.Y;

            var shapes = new TileShape[tw * th];
            for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
                shapes[ty * tw + tx] = TileShape.Of(map, tx, ty);

            var vertices = new List<Vector3>();
            var indices = new List<int>();

            void Line(float x0, float z0, int h0, float x1, float z1, int h1)
            {
                indices.Add(vertices.Count);
                indices.Add(vertices.Count + 1);
                vertices.Add(new Vector3(x0, h0 * TileShape.HeightUnitInTileWidths + Lift, z0));
                vertices.Add(new Vector3(x1, h1 * TileShape.HeightUnitInTileWidths + Lift, z1));
            }

            for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                var s = shapes[ty * tw + tx];

                // Bottom and left edges always; right and top only on the map border or where the
                // neighbor disagrees about the edge height (otherwise the neighbor already drew it).
                Line(tx, ty, s.H00, tx + 1, ty, s.H10);
                Line(tx, ty, s.H00, tx, ty + 1, s.H01);

                if (tx + 1 == tw || shapes[ty * tw + tx + 1].H00 != s.H10 || shapes[ty * tw + tx + 1].H01 != s.H11)
                    Line(tx + 1, ty, s.H10, tx + 1, ty + 1, s.H11);

                if (ty + 1 == th || shapes[(ty + 1) * tw + tx].H00 != s.H01 || shapes[(ty + 1) * tw + tx].H10 != s.H11)
                    Line(tx, ty + 1, s.H01, tx + 1, ty + 1, s.H11);
            }

            var mesh = new Mesh { name = "TerrainGrid" };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
