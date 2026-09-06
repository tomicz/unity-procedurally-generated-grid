using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace TOMICZ.Grid.Tests
{
    public class OptimizedGridTests
    {
        private static readonly Color32 Red = new Color32(255, 0, 0, 255);

        [Test]
        public void GenerateGrid_AllocatesFourVerticesAndSixIndicesPerNode()
        {
            OptimizedGrid grid = MakeGrid(3, 2);

            Assert.AreEqual(6, grid.NodeCount);
            Assert.AreEqual(24, grid.Vertices.Length);
            Assert.AreEqual(24, grid.Colors.Length);
            Assert.AreEqual(36, grid.Triangles.Length);
        }

        [TestCase(0, 5)]
        [TestCase(5, 0)]
        [TestCase(-1, 5)]
        public void GenerateGrid_WithNoNodes_ProducesEmptyBuffers(int width, int height)
        {
            OptimizedGrid grid = MakeGrid(width, height);

            Assert.AreEqual(0, grid.NodeCount);
            Assert.AreEqual(0, grid.Vertices.Length);
            Assert.AreEqual(0, grid.Triangles.Length);
            Assert.AreEqual(0, grid.Colors.Length);
        }

        [Test]
        public void GenerateGrid_IsCenteredOnOrigin()
        {
            OptimizedGrid grid = MakeGrid(4, 3, nodeWidth: 1f, nodeHeight: 2f, spacing: 0.5f);

            Bounds bounds = BoundsOf(grid.Vertices);

            // 4 nodes * (1 + 0.5) - 0.5 = 5.5 wide; 3 nodes * (2 + 0.5) - 0.5 = 7 tall.
            Assert.AreEqual(5.5f, bounds.size.x, 1e-4f);
            Assert.AreEqual(7f, bounds.size.y, 1e-4f);
            Assert.AreEqual(0f, bounds.center.x, 1e-4f);
            Assert.AreEqual(0f, bounds.center.y, 1e-4f);
        }

        [Test]
        public void GenerateGrid_Vertical_LiesInXYPlaneAndFacesNegativeZ()
        {
            OptimizedGrid grid = MakeGrid(2, 2, horizontal: false);

            foreach (Vector3 v in grid.Vertices)
            {
                Assert.AreEqual(0f, v.z);
            }

            Assert.Less(FirstTriangleNormal(grid).z, 0f);
        }

        [Test]
        public void GenerateGrid_Horizontal_LiesInXZPlaneAndFacesUp()
        {
            OptimizedGrid grid = MakeGrid(2, 2, horizontal: true);

            foreach (Vector3 v in grid.Vertices)
            {
                Assert.AreEqual(0f, v.y);
            }

            Assert.Greater(FirstTriangleNormal(grid).y, 0f);
        }

        [Test]
        public void GenerateGrid_AllIndicesAreInRange()
        {
            OptimizedGrid grid = MakeGrid(5, 4);

            foreach (int index in grid.Triangles)
            {
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, grid.Vertices.Length);
            }
        }

        [Test]
        public void GetNodeIndex_IsRowMajor()
        {
            OptimizedGrid grid = MakeGrid(7, 3);

            Assert.AreEqual(0, grid.GetNodeIndex(0, 0));
            Assert.AreEqual(6, grid.GetNodeIndex(6, 0));
            Assert.AreEqual(7, grid.GetNodeIndex(0, 1));
            Assert.AreEqual(20, grid.GetNodeIndex(6, 2));
        }

        [Test]
        public void GetNodeCenter_MatchesGeneratedQuadCenter()
        {
            OptimizedGrid grid = MakeGrid(4, 3, nodeWidth: 1f, nodeHeight: 2f, spacing: 0.5f);

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    int v = grid.GetNodeIndex(x, y) * OptimizedGrid.VerticesPerNode;
                    Vector3 quadCenter = (grid.Vertices[v] + grid.Vertices[v + 3]) * 0.5f;

                    Assert.AreEqual(0f, Vector3.Distance(quadCenter, grid.GetNodeCenter(x, y)), 1e-5f, $"node {x},{y}");
                }
            }
        }

        [Test]
        public void GetNodeCenter_Horizontal_LiesInXZPlane()
        {
            OptimizedGrid grid = MakeGrid(2, 2, horizontal: true);

            int v = grid.GetNodeIndex(1, 1) * OptimizedGrid.VerticesPerNode;
            Vector3 quadCenter = (grid.Vertices[v] + grid.Vertices[v + 3]) * 0.5f;
            Vector3 center = grid.GetNodeCenter(1, 1);

            Assert.AreEqual(0f, center.y);
            Assert.AreEqual(0f, Vector3.Distance(quadCenter, center), 1e-5f);
        }

        [Test]
        public void TryGetNode_RoundTripsThroughNodeCenter()
        {
            OptimizedGrid grid = MakeGrid(5, 4, spacing: 0.25f);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    Assert.IsTrue(grid.TryGetNode(grid.GetNodeCenter(x, y), out int rx, out int ry));
                    Assert.AreEqual(x, rx);
                    Assert.AreEqual(y, ry);
                }
            }
        }

        [Test]
        public void TryGetNode_PositionInSpacingGap_MapsToPrecedingNode()
        {
            OptimizedGrid grid = MakeGrid(3, 3, nodeWidth: 1f, nodeHeight: 1f, spacing: 0.5f);

            // Half a node to the right edge, then a quarter node into the gap.
            Vector3 inGap = grid.GetNodeCenter(1, 1) + new Vector3(0.75f, 0f, 0f);

            Assert.IsTrue(grid.TryGetNode(inGap, out int x, out int y));
            Assert.AreEqual(1, x);
            Assert.AreEqual(1, y);
        }

        [Test]
        public void TryGetNode_OutsideGrid_ReturnsFalse()
        {
            OptimizedGrid grid = MakeGrid(3, 3);

            Assert.IsFalse(grid.TryGetNode(new Vector3(100f, 0f, 0f), out _, out _));
            Assert.IsFalse(grid.TryGetNode(new Vector3(0f, -100f, 0f), out _, out _));
            Assert.IsFalse(grid.TryGetNode(grid.GetNodeCenter(-1, 0), out _, out _));
        }

        [Test]
        public void GetNodeCoordinates_InvertsGetNodeIndex()
        {
            OptimizedGrid grid = MakeGrid(7, 3);

            for (int i = 0; i < grid.NodeCount; i++)
            {
                grid.GetNodeCoordinates(i, out int x, out int y);
                Assert.AreEqual(i, grid.GetNodeIndex(x, y));
            }
        }

        [Test]
        public void GetNeighbors_InteriorNode_HasFourOrthogonalAndEightWithDiagonals()
        {
            OptimizedGrid grid = MakeGrid(5, 5);
            var buffer = new int[OptimizedGrid.MaxNeighbors];

            Assert.AreEqual(4, grid.GetNeighbors(2, 2, buffer));
            Assert.AreEqual(8, grid.GetNeighbors(2, 2, buffer, includeDiagonals: true));
        }

        [Test]
        public void GetNeighbors_CornerNode_StaysInBounds()
        {
            OptimizedGrid grid = MakeGrid(5, 5);
            var buffer = new int[OptimizedGrid.MaxNeighbors];

            int count = grid.GetNeighbors(0, 0, buffer, includeDiagonals: true);

            Assert.AreEqual(3, count);
            CollectionAssert.AreEquivalent(
                new[] { grid.GetNodeIndex(1, 0), grid.GetNodeIndex(0, 1), grid.GetNodeIndex(1, 1) },
                new[] { buffer[0], buffer[1], buffer[2] });
        }

        [Test]
        public void GetNeighbors_SkipsOccupiedNodesByDefault()
        {
            OptimizedGrid grid = MakeGrid(5, 5);
            var buffer = new int[OptimizedGrid.MaxNeighbors];
            grid.SetNodeOccupied(3, 2, true);

            int count = grid.GetNeighbors(2, 2, buffer);

            Assert.AreEqual(3, count);
            CollectionAssert.DoesNotContain(new[] { buffer[0], buffer[1], buffer[2] }, grid.GetNodeIndex(3, 2));
            Assert.AreEqual(4, grid.GetNeighbors(2, 2, buffer, skipOccupied: false));
        }

        [Test]
        public void GetNeighbors_DoesNotCutCornersAroundObstacles()
        {
            OptimizedGrid grid = MakeGrid(5, 5);
            var buffer = new int[OptimizedGrid.MaxNeighbors];
            // Block the node to the right; the two diagonals on that side must vanish.
            grid.SetNodeOccupied(3, 2, true);

            int count = grid.GetNeighbors(2, 2, buffer, includeDiagonals: true);

            Assert.AreEqual(5, count);
            var found = new int[count];
            System.Array.Copy(buffer, found, count);
            CollectionAssert.DoesNotContain(found, grid.GetNodeIndex(3, 1));
            CollectionAssert.DoesNotContain(found, grid.GetNodeIndex(3, 3));
        }

        [Test]
        public void ClearOccupancy_FreesEveryNode()
        {
            OptimizedGrid grid = MakeGrid(3, 3);
            grid.SetNodeOccupied(1, 1, true);
            grid.SetNodeOccupied(2, 0, true);

            grid.ClearOccupancy();

            foreach (bool occupied in grid.Occupied)
            {
                Assert.IsFalse(occupied);
            }
        }

        [Test]
        public void SetNodeColor_ByIndex_MatchesCoordinateOverload()
        {
            OptimizedGrid grid = MakeGrid(4, 4);

            grid.SetNodeColor(grid.GetNodeIndex(3, 1), Red);

            AssertColor(Red, grid.GetNodeColor(3, 1));
            AssertColor(Red, grid.Colors[grid.GetNodeIndex(3, 1) * OptimizedGrid.VerticesPerNode]);
        }

        [Test]
        public void SetNodeColor_UpdatesOnlyThatNodesFourVertices()
        {
            OptimizedGrid grid = MakeGrid(3, 3);

            grid.SetNodeColor(1, 2, Red);

            int first = grid.GetNodeIndex(1, 2) * OptimizedGrid.VerticesPerNode;
            for (int i = 0; i < grid.Colors.Length; i++)
            {
                bool inNode = i >= first && i < first + OptimizedGrid.VerticesPerNode;
                AssertColor(inNode ? Red : OptimizedGrid.White, grid.Colors[i], $"vertex {i}");
            }

            AssertColor(Red, grid.GetNodeColor(1, 2));
        }

        [TestCase(-1, 0)]
        [TestCase(3, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 3)]
        public void SetNodeColor_OutOfRange_IsIgnored(int x, int y)
        {
            OptimizedGrid grid = MakeGrid(3, 3);

            Assert.DoesNotThrow(() => grid.SetNodeColor(x, y, Red));

            foreach (Color32 c in grid.Colors)
            {
                AssertColor(OptimizedGrid.White, c);
            }
        }

        [Test]
        public void SetNodeColor_BeforeGenerate_IsAppliedWhenGenerated()
        {
            var grid = new OptimizedGrid(2, 2, 1f, 1f, 0f);

            grid.SetNodeColor(1, 1, Red);
            grid.GenerateGrid();

            int first = grid.GetNodeIndex(1, 1) * OptimizedGrid.VerticesPerNode;
            AssertColor(Red, grid.Colors[first]);
            AssertColor(Red, grid.Colors[first + 3]);
            AssertColor(OptimizedGrid.White, grid.Colors[0]);
        }

        [Test]
        public void Occupancy_RoundTripsAndRejectsOutOfRange()
        {
            OptimizedGrid grid = MakeGrid(4, 4);

            grid.SetNodeOccupied(2, 3, true);

            Assert.IsTrue(grid.IsNodeOccupied(2, 3));
            Assert.IsFalse(grid.IsNodeOccupied(3, 2));
            Assert.IsFalse(grid.IsNodeOccupied(-1, 0));
            Assert.IsFalse(grid.IsNodeOccupied(4, 0));
            Assert.DoesNotThrow(() => grid.SetNodeOccupied(4, 4, true));
        }

        [Test]
        public void Constructor_UsesSuppliedStateArraysInPlace()
        {
            var occupied = new bool[6];
            var colors = new Color32[6];
            var grid = new OptimizedGrid(3, 2, 1f, 1f, 0f, occupied, colors);

            grid.SetNodeOccupied(2, 1, true);
            grid.SetNodeColor(0, 1, Red);

            Assert.AreSame(occupied, grid.Occupied);
            Assert.AreSame(colors, grid.NodeColors);
            Assert.IsTrue(occupied[5]);
            AssertColor(Red, colors[3]);
        }

        [Test]
        public void Constructor_IgnoresStateArraysOfTheWrongSize()
        {
            var occupied = new bool[5];
            var colors = new Color32[5];
            var grid = new OptimizedGrid(3, 2, 1f, 1f, 0f, occupied, colors);

            Assert.AreNotSame(occupied, grid.Occupied);
            Assert.AreNotSame(colors, grid.NodeColors);
            Assert.AreEqual(6, grid.Occupied.Length);
            Assert.AreEqual(6, grid.NodeColors.Length);

            foreach (Color32 c in grid.NodeColors)
            {
                AssertColor(OptimizedGrid.White, c);
            }
        }

        [Test]
        public void SetNodeColor_MarksColorsDirtyUntilUploaded()
        {
            var mesh = new Mesh();
            try
            {
                OptimizedGrid grid = MakeGrid(3, 3);
                grid.LoadMeshData(mesh);
                Assert.IsFalse(grid.ColorsDirty, "clean after full upload");

                grid.SetNodeColor(0, 0, Red);
                Assert.IsTrue(grid.ColorsDirty, "dirty after a change");

                grid.LoadMeshColors(mesh);
                Assert.IsFalse(grid.ColorsDirty, "clean after color upload");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void SetAllNodeColors_FillsEveryNodeAndVertex()
        {
            OptimizedGrid grid = MakeGrid(3, 2);

            grid.SetAllNodeColors(Red);

            foreach (Color32 c in grid.NodeColors)
            {
                AssertColor(Red, c);
            }
            foreach (Color32 c in grid.Colors)
            {
                AssertColor(Red, c);
            }
            Assert.IsTrue(grid.ColorsDirty);
        }

        [Test]
        public void LoadMeshColors_WithMismatchedMesh_LeavesColorsDirty()
        {
            var mesh = new Mesh();
            try
            {
                MakeGrid(2, 2).LoadMeshData(mesh);
                OptimizedGrid other = MakeGrid(3, 3);
                other.SetNodeColor(0, 0, Red);

                other.LoadMeshColors(mesh);

                Assert.IsTrue(other.ColorsDirty);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void LoadMeshData_UsesSixteenBitIndicesForSmallGrids()
        {
            var mesh = new Mesh();
            try
            {
                MakeGrid(10, 10).LoadMeshData(mesh);

                Assert.AreEqual(IndexFormat.UInt16, mesh.indexFormat);
                Assert.AreEqual(400, mesh.vertexCount);
                Assert.AreEqual(600, (int)mesh.GetIndexCount(0));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void LoadMeshData_UsesThirtyTwoBitIndicesPastTheSixteenBitLimit()
        {
            var mesh = new Mesh();
            try
            {
                // 200 * 100 nodes = 80,000 vertices, past the 65,535 that 16-bit indices allow.
                MakeGrid(200, 100).LoadMeshData(mesh);

                Assert.AreEqual(IndexFormat.UInt32, mesh.indexFormat);
                Assert.AreEqual(80000, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void LoadMeshData_WithEmptyGrid_ClearsMesh()
        {
            var mesh = new Mesh();
            try
            {
                MakeGrid(4, 4).LoadMeshData(mesh);
                MakeGrid(0, 4).LoadMeshData(mesh);

                Assert.AreEqual(0, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void LoadMeshColors_UploadsColorsWithoutTouchingGeometry()
        {
            var mesh = new Mesh();
            try
            {
                OptimizedGrid grid = MakeGrid(3, 3);
                grid.LoadMeshData(mesh);
                Vector3[] verticesBefore = mesh.vertices;

                grid.SetNodeColor(2, 2, Red);
                grid.LoadMeshColors(mesh);

                int first = grid.GetNodeIndex(2, 2) * OptimizedGrid.VerticesPerNode;
                Color32[] uploaded = mesh.colors32;
                AssertColor(Red, uploaded[first]);
                AssertColor(OptimizedGrid.White, uploaded[0]);
                CollectionAssert.AreEqual(verticesBefore, mesh.vertices);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void LoadMeshColors_SkipsMeshWithMismatchedVertexCount()
        {
            var mesh = new Mesh();
            try
            {
                MakeGrid(2, 2).LoadMeshData(mesh);

                Assert.DoesNotThrow(() => MakeGrid(3, 3).LoadMeshColors(mesh));
                Assert.AreEqual(16, mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        private static OptimizedGrid MakeGrid(int width, int height, float nodeWidth = 1f, float nodeHeight = 1f,
            float spacing = 0.1f, bool horizontal = false)
        {
            var grid = new OptimizedGrid(width, height, nodeWidth, nodeHeight, spacing);
            grid.GenerateGrid(horizontal);
            return grid;
        }

        private static Bounds BoundsOf(Vector3[] vertices)
        {
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 v in vertices)
            {
                bounds.Encapsulate(v);
            }
            return bounds;
        }

        private static Vector3 FirstTriangleNormal(OptimizedGrid grid)
        {
            Vector3 a = grid.Vertices[grid.Triangles[0]];
            Vector3 b = grid.Vertices[grid.Triangles[1]];
            Vector3 c = grid.Vertices[grid.Triangles[2]];
            return Vector3.Cross(b - a, c - a);
        }

        private static void AssertColor(Color32 expected, Color32 actual, string message = null)
        {
            Assert.AreEqual(expected.r, actual.r, message);
            Assert.AreEqual(expected.g, actual.g, message);
            Assert.AreEqual(expected.b, actual.b, message);
            Assert.AreEqual(expected.a, actual.a, message);
        }
    }
}
