using NUnit.Framework;
using Polis.Unity;
using UnityEngine;

namespace Polis.Sim.Tests
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
                Assert.AreEqual((flatEdges + 1) * 2, mesh.vertexCount); // the shared edge is drawn twice
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
