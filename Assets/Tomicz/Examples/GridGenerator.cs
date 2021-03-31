using UnityEngine;
using Tomicz.Grid;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridGenerator : MonoBehaviour
{
    [SerializeField] private int gridWidth;

    [SerializeField] private int gridHeight;

    [SerializeField] private float spacing = .1f;
    
    [SerializeField] private float nodeWidth = 1f;

    [SerializeField] private float nodeHeight = 1f;

    [SerializeField] private GridPosition gridPosition;

    private Mesh _mesh = null;

    private OptimizedGrid _grid;

    private void Awake()
    {
        _mesh = GetComponent<MeshFilter>().sharedMesh;
        _mesh.name = "Grid Mesh";
    }

    private void OnValidate()
    {
        _grid = new OptimizedGrid(gridWidth, gridHeight, nodeWidth, nodeHeight, spacing);
        _grid.GenerateGrid(gridPosition);

        LoadMeshData();
    }

    private void LoadMeshData()
    {
        _mesh.Clear();

        _mesh.vertices = _grid.Vertices;
        _mesh.triangles = _grid.Triangles;

        _mesh.RecalculateNormals();
    }
}
