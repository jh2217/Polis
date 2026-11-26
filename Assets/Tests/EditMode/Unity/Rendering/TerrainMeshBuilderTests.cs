using NUnit.Framework;
using Polis.Sim;
using Polis.Sim.Grids;
using Polis.Sim.Terrain;
using Polis.Unity;
using Polis.Unity.Rendering;
using UnityEngine;

namespace Polis.Unity.Tests.Rendering
{
    public class TerrainMeshBuilderTests
    {
        private static int CountVerticesFacing(Mesh mesh, Vector3 direction)
        {
            int count = 0;
            foreach (var n in mesh.normals)
                if (Vector3.Dot(n, direction) > 0.99f) count++;
            return count;
        }

        [Test]
        public void Build_FlatMap_HasOneQuadPerTile_AndNoWalls()
        {
            var mesh = TerrainMeshBuilder.Build(new HeightMap(5, 4));
            try
            {
                Assert.AreEqual(20 * 4, mesh.vertexCount);
                Assert.AreEqual(20 * 6, mesh.triangles.Length);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_FlatMap_FacesUp()
        {
            var mesh = TerrainMeshBuilder.Build(new HeightMap(4, 4));
            try
            {
                foreach (var n in mesh.normals)
                    Assert.AreEqual(1f, n.y, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_HeightUnits_AreScaledToTileWidths()
        {
            var map = new HeightMap(1, 1);
            map[1, 1] = TileShape.MaxSlope; // within the slope limit, so no clamping

            var mesh = TerrainMeshBuilder.Build(map);
            try
            {
                Assert.AreEqual(TileShape.MaxSlope * TileShape.HeightUnitInTileWidths, mesh.bounds.max.y, 1e-4f);
                Assert.AreEqual(1f, mesh.bounds.max.x, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_GentleSlope_HasNoWalls()
        {
            var map = new HeightMap(2, 1);
            map[2, 0] = TileShape.MaxSlope;
            map[2, 1] = TileShape.MaxSlope; // right-hand column rises by exactly the slope limit

            var mesh = TerrainMeshBuilder.Build(map);
            try
            {
                Assert.AreEqual(2 * 4, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_SteepStepAlongX_AddsOneWallFacingTheLowerTile()
        {
            int peak = TileShape.MaxSlope + 1; // the smallest rise that is a cliff
            var map = new HeightMap(2, 1);
            map[2, 0] = peak;
            map[2, 1] = peak; // right tile clamps to peak - MaxSlope..peak, left tile stays at 0

            var mesh = TerrainMeshBuilder.Build(map);
            try
            {
                Assert.AreEqual(3 * 4, mesh.vertexCount); // two tops plus one wall
                Assert.AreEqual(4, CountVerticesFacing(mesh, Vector3.left));
                Assert.AreEqual(peak * TileShape.HeightUnitInTileWidths, mesh.bounds.max.y, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_SteepStepAlongY_WallFacesTheLowerTile()
        {
            int peak = TileShape.MaxSlope + 1; // the smallest rise that is a cliff
            var map = new HeightMap(1, 2);
            map[0, 2] = peak;
            map[1, 2] = peak; // far tile clamps to peak - MaxSlope..peak, near tile stays at 0

            var mesh = TerrainMeshBuilder.Build(map);
            try
            {
                Assert.AreEqual(3 * 4, mesh.vertexCount);
                Assert.AreEqual(4, CountVerticesFacing(mesh, Vector3.back));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Build_Uvs_PointAtTheVertexPixelCenter()
        {
            var mesh = TerrainMeshBuilder.Build(new HeightMap(4, 2));
            try
            {
                // The first vertex is tile (0,0)'s corner at vertex (0,0); the texture is 5 x 3 pixels.
                Assert.AreEqual(new Vector2(0.5f / 5f, 0.5f / 3f), mesh.uv[0]);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
