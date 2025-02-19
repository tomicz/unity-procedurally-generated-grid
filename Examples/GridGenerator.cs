using UnityEngine;
using TOMICZ.Grid;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridGenerator : MonoBehaviour
{
    public OptimizedGrid Grid => _grid;

    [SerializeField] private int gridWidth;
    [SerializeField] private int gridHeight;
    [SerializeField] private float spacing = .1f;
    [SerializeField] private float nodeWidth = 1f;
    [SerializeField] private float nodeHeight = 1f;
    [SerializeField] private bool isHorizontal;

    private Mesh _mesh;
    private OptimizedGrid _grid;
    private List<MeshFilter> _additionalMeshFilters = new();

    public bool IsHorizontal => isHorizontal;

    public void SetNodeColor(int x, int y, Color color)
    {
        if (_grid != null)
        {
            _grid.SetNodeColor(x, y, color);
            
            // Update all mesh sections
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
            _mesh.name = "Grid Mesh";
            meshFilter.sharedMesh = _mesh;
        }
        else
        {
            _mesh = meshFilter.sharedMesh;
        }

        Material gridMaterial = new Material(Shader.Find("Custom/VertexColor"));
        Renderer renderer = GetComponent<Renderer>();

        if(renderer != null)
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

        // Ensure positive values
        gridWidth = Mathf.Max(0, gridWidth);
        gridHeight = Mathf.Max(0, gridHeight);

        RegenerateGrid();
    }

    private void RegenerateGrid()
    {
        // First, destroy all existing section objects and clear the list
        ClearAdditionalMeshes();

        // Clear the main mesh
        if (_mesh != null)
        {
            _mesh.Clear();
            // Ensure the main mesh filter still has our mesh
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = _mesh;
            }
        }

        // Generate completely new grid
        _grid = new OptimizedGrid(gridWidth, gridHeight, nodeWidth, nodeHeight, spacing);
        _grid.GenerateGrid(isHorizontal);

        // Load first section into main mesh
        _grid.LoadMeshData(_mesh);

        // Create new section objects only if needed
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
        // Ensure we destroy both the GameObjects and their meshes
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
