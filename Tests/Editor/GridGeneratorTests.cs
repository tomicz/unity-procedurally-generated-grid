using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TOMICZ.Grid.Tests
{
    public class GridGeneratorTests
    {
        private static readonly Color32 Red = new Color32(255, 0, 0, 255);

        private GameObject _gameObject;
        private GridGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Grid Under Test");
            // ExecuteInEditMode: OnEnable runs here and builds the default 10 x 10 grid.
            _generator = _gameObject.AddComponent<GridGenerator>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void AddingComponent_BuildsGridAndMesh()
        {
            Assert.IsNotNull(_generator.Grid);
            Assert.AreEqual(100, _generator.Grid.NodeCount);

            Mesh mesh = SharedMesh();
            Assert.IsNotNull(mesh);
            Assert.AreEqual(400, mesh.vertexCount);
        }

        [Test]
        public void GeneratedMesh_IsNotSavedIntoTheScene()
        {
            Assert.AreNotEqual(0, (int)(SharedMesh().hideFlags & HideFlags.DontSave));
        }

        [Test]
        public void SetNodeColor_UpdatesMeshAndSurvivesRegenerate()
        {
            _generator.SetNodeColor(2, 3, Red);
            _generator.RegenerateGrid();

            AssertColor(Red, _generator.GetNodeColor(2, 3));

            int first = _generator.Grid.GetNodeIndex(2, 3) * OptimizedGrid.VerticesPerNode;
            AssertColor(Red, SharedMesh().colors32[first]);
        }

        [Test]
        public void SetNodeOccupied_SurvivesRegenerate()
        {
            _generator.SetNodeOccupied(4, 4, true);
            _generator.RegenerateGrid();

            Assert.IsTrue(_generator.IsNodeOccupied(4, 4));
            Assert.IsFalse(_generator.IsNodeOccupied(4, 5));
        }

        [Test]
        public void Resizing_KeepsStateInTheOverlappingRegion()
        {
            _generator.SetNodeOccupied(1, 1, true);
            _generator.SetNodeColor(3, 2, Red);

            SetDimensions(12, 8);
            _generator.RegenerateGrid();

            Assert.AreEqual(12, _generator.Grid.GridWidth);
            Assert.AreEqual(8, _generator.Grid.GridHeight);
            Assert.IsTrue(_generator.IsNodeOccupied(1, 1));
            AssertColor(Red, _generator.GetNodeColor(3, 2));
            AssertColor(OptimizedGrid.White, _generator.GetNodeColor(11, 7));
            Assert.AreEqual(12 * 8 * OptimizedGrid.VerticesPerNode, SharedMesh().vertexCount);
        }

        [Test]
        public void Resizing_ThenGrowingBack_DoesNotResurrectDroppedState()
        {
            _generator.SetNodeOccupied(9, 9, true);

            SetDimensions(5, 5);
            _generator.RegenerateGrid();
            SetDimensions(10, 10);
            _generator.RegenerateGrid();

            Assert.IsFalse(_generator.IsNodeOccupied(9, 9));
        }

        [Test]
        public void WorldToNode_AccountsForTheTransform()
        {
            _gameObject.transform.SetPositionAndRotation(new Vector3(10f, 0f, -3f), Quaternion.Euler(0f, 0f, 90f));

            Vector3 world = _generator.NodeToWorld(7, 2);

            Assert.IsTrue(_generator.WorldToNode(world, out int x, out int y));
            Assert.AreEqual(7, x);
            Assert.AreEqual(2, y);
            Assert.IsFalse(_generator.WorldToNode(new Vector3(1000f, 0f, 0f), out _, out _));
        }

        [Test]
        public void SetNodeColor_InEditMode_UploadsImmediately()
        {
            _generator.SetNodeColor(1, 1, Red);

            Assert.IsFalse(_generator.Grid.ColorsDirty);
            int first = _generator.Grid.GetNodeIndex(1, 1) * OptimizedGrid.VerticesPerNode;
            AssertColor(Red, SharedMesh().colors32[first]);
        }

        [Test]
        public void SetAllNodeColors_RepaintsWholeMesh()
        {
            _generator.SetNodeColor(0, 0, Red);
            _generator.SetAllNodeColors(OptimizedGrid.White);

            foreach (Color32 c in SharedMesh().colors32)
            {
                AssertColor(OptimizedGrid.White, c);
            }
        }

        [Test]
        public void SaveLayout_ThenLoadLayout_RestoresStateAndDimensions()
        {
            var layout = ScriptableObject.CreateInstance<GridLayoutAsset>();
            try
            {
                _generator.SetNodeOccupied(2, 2, true);
                _generator.SetNodeColor(7, 3, Red);
                _generator.SaveLayout(layout);

                SetDimensions(5, 5);
                _generator.RegenerateGrid();
                _generator.SetNodeOccupied(0, 0, true);

                Assert.IsTrue(_generator.LoadLayout(layout));

                Assert.AreEqual(10, _generator.Grid.GridWidth);
                Assert.AreEqual(10, _generator.Grid.GridHeight);
                Assert.IsTrue(_generator.IsNodeOccupied(2, 2));
                Assert.IsFalse(_generator.IsNodeOccupied(0, 0));
                AssertColor(Red, _generator.GetNodeColor(7, 3));
                Assert.AreEqual(400, SharedMesh().vertexCount);
                AssertColor(Red, SharedMesh().colors32[_generator.Grid.GetNodeIndex(7, 3) * OptimizedGrid.VerticesPerNode]);
            }
            finally
            {
                Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void LoadLayout_Null_ReturnsFalse()
        {
            Assert.IsFalse(_generator.LoadLayout(null));
        }

        [Test]
        public void NodeColors_SerializeAsPackedBytesAndSurviveARoundTrip()
        {
            _generator.SetNodeColor(4, 4, Red);

            var serialized = new SerializedObject(_generator);
            SerializedProperty bytes = serialized.FindProperty("_nodeColorBytes");

            Assert.IsNotNull(bytes, "packed color field is serialized");
            Assert.AreEqual(100 * Color32Packing.BytesPerColor, bytes.arraySize);
            Assert.IsNull(serialized.FindProperty("_nodeColors"), "Color32 array is not serialized");

            // Applying any change deserializes the component again; the live grid must keep its colors.
            SerializedProperty gizmos = serialized.FindProperty("_drawOccupiedGizmos");
            gizmos.boolValue = !gizmos.boolValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssertColor(Red, _generator.GetNodeColor(4, 4));
        }

        [Test]
        public void SetNodeColor_OutOfRange_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _generator.SetNodeColor(10, 0, Red));
            Assert.DoesNotThrow(() => _generator.SetNodeColor(-1, -1, Red));
        }

        private Mesh SharedMesh()
        {
            return _gameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private void SetDimensions(int width, int height)
        {
            var serialized = new SerializedObject(_generator);
            serialized.FindProperty("_gridWidth").intValue = width;
            serialized.FindProperty("_gridHeight").intValue = height;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
