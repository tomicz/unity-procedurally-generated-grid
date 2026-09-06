using System.Collections.Generic;
using NUnit.Framework;

namespace TOMICZ.Grid.Tests
{
    public class GridPathfinderTests
    {
        [Test]
        public void OpenGrid_WithoutDiagonals_PathHasManhattanLength()
        {
            OptimizedGrid grid = MakeGrid(10, 10);
            var path = new List<int>();

            bool found = new GridPathfinder(grid, allowDiagonals: false).FindPath(0, 0, 9, 9, path);

            Assert.IsTrue(found);
            Assert.AreEqual(19, path.Count);
            Assert.AreEqual(grid.GetNodeIndex(0, 0), path[0]);
            Assert.AreEqual(grid.GetNodeIndex(9, 9), path[path.Count - 1]);
            AssertStepsAreNeighbors(grid, path, allowDiagonals: false);
        }

        [Test]
        public void OpenGrid_WithDiagonals_PathIsStraightDiagonal()
        {
            OptimizedGrid grid = MakeGrid(10, 10);
            var path = new List<int>();

            bool found = new GridPathfinder(grid, allowDiagonals: true).FindPath(0, 0, 9, 9, path);

            Assert.IsTrue(found);
            Assert.AreEqual(10, path.Count);
            AssertStepsAreNeighbors(grid, path, allowDiagonals: true);
        }

        [Test]
        public void StartEqualsEnd_ReturnsSingleNode()
        {
            OptimizedGrid grid = MakeGrid(4, 4);
            var path = new List<int>();

            Assert.IsTrue(new GridPathfinder(grid).FindPath(2, 2, 2, 2, path));
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(grid.GetNodeIndex(2, 2), path[0]);
        }

        [Test]
        public void OccupiedEndpoint_ReturnsFalseAndEmptyPath()
        {
            OptimizedGrid grid = MakeGrid(4, 4);
            grid.SetNodeOccupied(3, 3, true);
            var path = new List<int> { 99 };

            Assert.IsFalse(new GridPathfinder(grid).FindPath(0, 0, 3, 3, path));
            Assert.IsEmpty(path);
        }

        [Test]
        public void OutOfBoundsEndpoint_ReturnsFalse()
        {
            OptimizedGrid grid = MakeGrid(4, 4);
            var path = new List<int>();

            Assert.IsFalse(new GridPathfinder(grid).FindPath(0, 0, 4, 0, path));
            Assert.IsFalse(new GridPathfinder(grid).FindPath(-1, 0, 1, 1, path));
        }

        [Test]
        public void WallWithGap_PathGoesThroughTheGap()
        {
            OptimizedGrid grid = MakeGrid(7, 7);
            for (int y = 0; y < 6; y++)
            {
                grid.SetNodeOccupied(3, y, true);
            }
            var path = new List<int>();

            bool found = new GridPathfinder(grid, allowDiagonals: false).FindPath(0, 0, 6, 0, path);

            Assert.IsTrue(found);
            CollectionAssert.Contains(path, grid.GetNodeIndex(3, 6));
            foreach (int node in path)
            {
                Assert.IsFalse(grid.Occupied[node], $"path crosses occupied node {node}");
            }
            AssertStepsAreNeighbors(grid, path, allowDiagonals: false);
        }

        [Test]
        public void FullWall_ReturnsFalse()
        {
            OptimizedGrid grid = MakeGrid(7, 7);
            for (int y = 0; y < 7; y++)
            {
                grid.SetNodeOccupied(3, y, true);
            }
            var path = new List<int>();

            Assert.IsFalse(new GridPathfinder(grid).FindPath(0, 0, 6, 0, path));
            Assert.IsEmpty(path);
        }

        [Test]
        public void BlockedOrthogonals_PreventDiagonalEscape()
        {
            // (0,0) is boxed in by (1,0) and (0,1). Without the corner-cutting rule
            // the diagonal to (1,1) would still be open.
            OptimizedGrid grid = MakeGrid(3, 3);
            grid.SetNodeOccupied(1, 0, true);
            grid.SetNodeOccupied(0, 1, true);
            var path = new List<int>();

            Assert.IsFalse(new GridPathfinder(grid, allowDiagonals: true).FindPath(0, 0, 2, 2, path));
        }

        [Test]
        public void ConsecutiveSearches_ReuseTheSamePathfinder()
        {
            OptimizedGrid grid = MakeGrid(8, 8);
            var pathfinder = new GridPathfinder(grid, allowDiagonals: false);
            var path = new List<int>();

            Assert.IsTrue(pathfinder.FindPath(0, 0, 7, 0, path));
            Assert.AreEqual(8, path.Count);

            for (int y = 0; y < 7; y++)
            {
                grid.SetNodeOccupied(4, y, true);
            }

            Assert.IsTrue(pathfinder.FindPath(0, 0, 7, 0, path));
            Assert.Greater(path.Count, 8);
            CollectionAssert.Contains(path, grid.GetNodeIndex(4, 7));

            Assert.IsTrue(pathfinder.FindPath(7, 7, 0, 7, path));
            Assert.AreEqual(grid.GetNodeIndex(7, 7), path[0]);
            Assert.AreEqual(grid.GetNodeIndex(0, 7), path[path.Count - 1]);
        }

        [Test]
        public void VisitedTracking_ReflectsTheLastSearch()
        {
            OptimizedGrid grid = MakeGrid(6, 6);
            var pathfinder = new GridPathfinder(grid);
            var path = new List<int>();

            Assert.IsFalse(pathfinder.WasVisited(0), "nothing visited before a search");

            pathfinder.FindPath(0, 0, 5, 5, path);

            Assert.Greater(pathfinder.LastVisitedCount, 0);
            Assert.IsTrue(pathfinder.WasVisited(grid.GetNodeIndex(0, 0)), "start is expanded");
            Assert.IsFalse(pathfinder.WasVisited(-1));
            Assert.IsFalse(pathfinder.WasVisited(grid.NodeCount));
        }

        private static OptimizedGrid MakeGrid(int width, int height)
        {
            var grid = new OptimizedGrid(width, height, 1f, 1f, 0f);
            grid.GenerateGrid();
            return grid;
        }

        private static void AssertStepsAreNeighbors(OptimizedGrid grid, List<int> path, bool allowDiagonals)
        {
            for (int i = 1; i < path.Count; i++)
            {
                grid.GetNodeCoordinates(path[i - 1], out int ax, out int ay);
                grid.GetNodeCoordinates(path[i], out int bx, out int by);
                int dx = System.Math.Abs(ax - bx);
                int dy = System.Math.Abs(ay - by);

                Assert.LessOrEqual(dx, 1, $"step {i} moves too far in x");
                Assert.LessOrEqual(dy, 1, $"step {i} moves too far in y");
                Assert.AreNotEqual(0, dx + dy, $"step {i} does not move");
                if (!allowDiagonals)
                {
                    Assert.AreEqual(1, dx + dy, $"step {i} is diagonal");
                }
            }
        }
    }
}
