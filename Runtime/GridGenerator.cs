using System.Collections.Generic;
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
        private List<MeshFilter> _additionalMeshFilters = new();

        public OptimizedGrid Grid => _grid;
        public bool IsHorizontal => _isHorizontal;

        public void SetNodeColor(int x, int y, Color color)
        {
            if (_grid != null)
            {
                _grid.SetNodeColor(x, y, color);

                _grid.LoadMeshData(_mesh);
                for (int i = 0; i < _additionalMeshFilters.Count; i++)
                {
                    if (_additionalMeshFilters[i] != null && i + 1 < _grid.MeshSections.Count)
                    {
                        var section = _grid.MeshSections[i + 1];
                        var sectionMesh = _additionalMeshFilters[i].sharedMesh;
                        if (sectionMesh != null)
                        {
                            sectionMesh.colors = section.Colors.ToArray();
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "Grid Mesh Section 0";
                meshFilter.sharedMesh = _mesh;
            }
            else
            {
                _mesh = meshFilter.sharedMesh;
            }

            Material gridMaterial = new Material(Shader.Find("Custom/VertexColor"));
            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material = gridMaterial;
            }
        }

        private void OnValidate()
        {
            if (_mesh == null)
            {
                var meshFilter = GetComponent<MeshFilter>();
                if (meshFilter.sharedMesh == null)
                {
                    _mesh = new Mesh();
                    _mesh.name = "Grid Mesh Section 0";
                    meshFilter.sharedMesh = _mesh;
                }
                else
                {
                    _mesh = meshFilter.sharedMesh;
                }
            }

            _gridWidth = Mathf.Max(0, _gridWidth);
            _gridHeight = Mathf.Max(0, _gridHeight);

            RegenerateGrid();
        }

        private void RegenerateGrid()
        {
            ClearAdditionalMeshes();

            if (_mesh != null)
            {
                _mesh.Clear();
                var meshFilter = GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = _mesh;
                }
            }

            _grid = new OptimizedGrid(_gridWidth, _gridHeight, _nodeWidth, _nodeHeight, _spacing);
            _grid.GenerateGrid(_isHorizontal);

            _grid.LoadMeshData(_mesh);

            for (int i = 1; i < _grid.MeshSections.Count; i++)
            {
                var go = new GameObject($"Grid Section {i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.material = GetComponent<Renderer>().sharedMaterial;

                var sectionMesh = new Mesh();
                sectionMesh.name = $"Grid Mesh Section {i}";
                mf.sharedMesh = sectionMesh;

                var section = _grid.MeshSections[i];
                sectionMesh.vertices = section.Vertices.ToArray();
                sectionMesh.triangles = section.Triangles.ToArray();
                sectionMesh.colors = section.Colors.ToArray();
                sectionMesh.RecalculateNormals();

                _additionalMeshFilters.Add(mf);
            }
        }

        private void ClearAdditionalMeshes()
        {
            foreach (var filter in _additionalMeshFilters)
            {
                if (filter != null)
                {
                    if (filter.sharedMesh != null)
                    {
                        if (Application.isPlaying)
                            Destroy(filter.sharedMesh);
                        else
                            DestroyImmediate(filter.sharedMesh);
                    }

                    if (filter.gameObject != null)
                    {
                        if (Application.isPlaying)
                            Destroy(filter.gameObject);
                        else
                            DestroyImmediate(filter.gameObject);
                    }
                }
            }
            _additionalMeshFilters.Clear();
        }
    }
}
