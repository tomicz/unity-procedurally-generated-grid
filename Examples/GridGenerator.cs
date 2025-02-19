using UnityEngine;
using TOMICZ.Grid;

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

    public void SetNodeColor(int x, int y, Color color)
    {
        if (_grid != null)
        {
            _grid.SetNodeColor(x, y, color);
            _grid.LoadMeshData(_mesh);
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
                _mesh.name = "Grid Mesh";
                meshFilter.sharedMesh = _mesh;
            }
            else
            {
                _mesh = meshFilter.sharedMesh;
            }
        }

        _grid = new OptimizedGrid(gridWidth, gridHeight, nodeWidth, nodeHeight, spacing);
        _grid.GenerateGrid(isHorizontal);
        _grid.LoadMeshData(_mesh);
    }
}
