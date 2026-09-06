using NUnit.Framework;
using UnityEngine;

namespace TOMICZ.Grid.Tests
{
    public class GridLayoutAssetTests
    {
        private static readonly Color32 Red = new Color32(255, 0, 0, 255);

        private GridLayoutAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<GridLayoutAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void NewAsset_IsValidAndEmpty()
        {
            Assert.IsTrue(_asset.IsValid);
            Assert.AreEqual(0, _asset.NodeCount);
        }

        [Test]
        public void CopyFrom_SnapshotsDimensionsOccupancyAndColors()
        {
            OptimizedGrid grid = MakeGrid(4, 3);
            grid.SetNodeOccupied(1, 2, true);
            grid.SetNodeColor(3, 0, Red);

            _asset.CopyFrom(grid);

            Assert.AreEqual(4, _asset.Width);
            Assert.AreEqual(3, _asset.Height);
            Assert.IsTrue(_asset.IsValid);

            // Later changes to the grid must not leak into the snapshot.
            grid.SetNodeOccupied(0, 0, true);
            OptimizedGrid restored = MakeGrid(4, 3);
            Assert.IsTrue(_asset.CopyTo(restored));
            Assert.IsTrue(restored.IsNodeOccupied(1, 2));
            Assert.IsFalse(restored.IsNodeOccupied(0, 0));
            AssertColor(Red, restored.GetNodeColor(3, 0));
        }

        [Test]
        public void CopyTo_UpdatesVertexColorsAndMarksDirty()
        {
            OptimizedGrid source = MakeGrid(2, 2);
            source.SetNodeColor(1, 1, Red);
            _asset.CopyFrom(source);

            OptimizedGrid target = MakeGrid(2, 2);
            var mesh = new Mesh();
            try
            {
                target.LoadMeshData(mesh);
                Assert.IsTrue(_asset.CopyTo(target));

                Assert.IsTrue(target.ColorsDirty);
                AssertColor(Red, target.Colors[target.GetNodeIndex(1, 1) * OptimizedGrid.VerticesPerNode]);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void CopyTo_RejectsGridOfDifferentSize()
        {
            _asset.CopyFrom(MakeGrid(4, 4));
            OptimizedGrid other = MakeGrid(3, 4);
            other.SetNodeOccupied(0, 0, true);

            Assert.IsFalse(_asset.CopyTo(other));
            Assert.IsTrue(other.IsNodeOccupied(0, 0), "grid left untouched");
        }

        private static OptimizedGrid MakeGrid(int width, int height)
        {
            var grid = new OptimizedGrid(width, height, 1f, 1f, 0f);
            grid.GenerateGrid();
            return grid;
        }

        private static void AssertColor(Color32 expected, Color32 actual)
        {
            Assert.AreEqual(expected.r, actual.r);
            Assert.AreEqual(expected.g, actual.g);
            Assert.AreEqual(expected.b, actual.b);
            Assert.AreEqual(expected.a, actual.a);
        }
    }
}
