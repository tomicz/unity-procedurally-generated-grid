using UnityEngine;
using UnityEngine.Serialization;

namespace TOMICZ.Grid
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GridGenerator : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("gridWidth")] private int _gridWidth = 10;
        [SerializeField, FormerlySerializedAs("gridHeight")] private int _gridHeight = 10;
        [SerializeField, FormerlySerializedAs("spacing")] private float _spacing = .1f;
        [SerializeField, FormerlySerializedAs("nodeWidth")] private float _nodeWidth = 1f;
        [SerializeField, FormerlySerializedAs("nodeHeight")] private float _nodeHeight = 1f;
        [SerializeField, FormerlySerializedAs("isHorizontal")] private bool _isHorizontal;
        [SerializeField] private Material _defaultMaterial;

        private Mesh _mesh;
        private OptimizedGrid _grid;

        public OptimizedGrid Grid => _grid;
        public bool IsHorizontal => _isHorizontal;

        public void SetNodeColor(int x, int y, Color color)
        {
            if (_grid == null) return;

            _grid.SetNodeColor(x, y, color);
            _grid.LoadMeshData(_mesh);
        }

        private void Awake()
        {
            EnsureMesh();

            Material gridMaterial = new Material(Shader.Find("Custom/VertexColor"));
            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material = gridMaterial;
            }
        }

        private void OnValidate()
        {
            EnsureMesh();

            _gridWidth = Mathf.Max(0, _gridWidth);
            _gridHeight = Mathf.Max(0, _gridHeight);

            RegenerateGrid();
        }

        private void EnsureMesh()
        {
            if (_mesh != null) return;

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "Grid Mesh";
                meshFilter.sharedMesh = _mesh;
            }
            else
            {
                _mesh = meshFilter.sharedMesh;
            }
        }

        private void RegenerateGrid()
        {
            _grid = new OptimizedGrid(_gridWidth, _gridHeight, _nodeWidth, _nodeHeight, _spacing);
            _grid.GenerateGrid(_isHorizontal);
            _grid.LoadMeshData(_mesh);
        }
    }
}
