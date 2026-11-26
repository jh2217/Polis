using NUnit.Framework;
using Polis.Sim;
using Polis.Sim.Grids;
using Polis.Sim.Terrain;
using Polis.Unity;
using Polis.Unity.Rendering;
using UnityEngine;

namespace Polis.Unity.Tests.Rendering
{
    public class TerrainGridBuilderTests
    {
        [Test]
        public void Build_FlatMap_DrawsEachTileEdgeOnce()
        {
            var mesh = TerrainGridBuilder.Build(new HeightMap(3, 2));
            try
            {
                int edges = (3 + 1) * 2 + 3 * (2 + 1); // vertical lines plus horizontal lines
                Assert.AreEqual(MeshTopology.Lines, mesh.GetTopology(0));
                Assert.AreEqual(edges * 2, mesh.vertexCount);
                Assert.AreEqual(edges * 2, mesh.GetIndexCount(0));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_Lines_SitJustAboveTheSurface()
        {
            var map = new HeightMap(1, 1);
            map[1, 1] = TileShape.MaxSlope;

            var mesh = TerrainGridBuilder.Build(map);
            try
            {
                float top = TileShape.MaxSlope * TileShape.HeightUnitInTileWidths;
                Assert.Greater(mesh.bounds.max.y, top);
                Assert.Less(mesh.bounds.max.y, top + 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_CliffBoundary_DrawsBothSidesOfTheWall()
        {
            int peak = TileShape.MaxSlope + 1; // the smallest rise that is a cliff
            var map = new HeightMap(2, 1);
            map[2, 0] = peak;
            map[2, 1] = peak;

            var mesh = TerrainGridBuilder.Build(map);
            try
            {
                int flatEdges = 3 + 4; // a 2 x 1 grid has 3 vertical and 4 horizontal edges
                int wallSides = 2; // the wall's two vertical edges, at the ends of the shared edge
                // The shared edge is drawn twice.
                Assert.AreEqual((flatEdges + 1 + wallSides) * 2, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_CliffBoundary_DrawsVerticalLinesAtTheWallEnds()
        {
            int peak = TileShape.MaxSlope + 1;
            var map = new HeightMap(2, 1);
            map[2, 0] = peak;
            map[2, 1] = peak;

            var mesh = TerrainGridBuilder.Build(map);
            try
            {
                var vertices = mesh.vertices;
                var indices = mesh.GetIndices(0);
                int vertical = 0;
                for (int i = 0; i < indices.Length; i += 2)
                {
                    var a = vertices[indices[i]];
                    var b = vertices[indices[i + 1]];
                    if (Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.z, b.z) && !Mathf.Approximately(a.y, b.y))
                        vertical++;
                }
                Assert.AreEqual(2, vertical);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_FlatMap_HasNoVerticalLines()
        {
            var mesh = TerrainGridBuilder.Build(new HeightMap(3, 3));
            try
            {
                Assert.AreEqual(0f, mesh.bounds.size.y, 1e-5f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
