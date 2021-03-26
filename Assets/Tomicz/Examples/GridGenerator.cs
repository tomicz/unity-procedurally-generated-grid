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

    private Mesh mesh = null;

    private OptimizedGrid _grid;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.name = "Grid Mesh";
    }

    private void Update()
    {
        _grid = new OptimizedGrid(gridWidth, gridHeight, nodeWidth, nodeHeight, spacing);
        _grid.GenerateGrid();

        LoadMeshData();
    }

    private void LoadMeshData()
    {
        mesh.vertices = _grid.Vertices;
        mesh.triangles = _grid.Triangles;

        mesh.RecalculateNormals();
    }
}
