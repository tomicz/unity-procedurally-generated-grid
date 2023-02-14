
<p align="center">
<img src="https://media.giphy.com/media/mdFObXCvTojzmWnQvf/giphy.gif" alt="Grid animation" title="Grid GIF" width="500"/>
</p>

# About

The Optimized Grid is a procedurally generated grid system that boasts exceptional performance, making it ideal for individuals seeking to learn about optimization and memory management. This tool eliminates the need for object pooling with Unity's GameObjects, making it a perfect tool for those looking to practice pathfinding."
# Getting started

Do not forget to add a namespace.
```
using Tomicz.Grid;

```	
Before proceeding, please ensure that the necessary reference dependencies have been added. Specifically, confirm that both MeshFilter and MeshRenderer components have been appropriately attached to the designated gameObject.
```
private Mesh _mesh;
private OptimizedGrid _grid;
```
If required for real-time updates in your application or game, you can invoke this function within an Update loop with the [ExecuteInEditMode] attribute. For optimal performance, it is recommended to update the grid using sliders or alternative input methods.
```		
private void Start()
{
	// Get dependencies
	_mesh = GetComponent<MeshFilter>().sharedMesh;

	// Create an instance of a grid
	_grid = new OptimizedGrid(gridWidth, gridHeight, nodeWidth, nodeHeight, spacing);

	// Always clal after the instance.
	_grid.GenerateGrid();
	
	// Loads vertices and triangles to a mesh
	LoadMeshData(_mesh);
}

```

By calling this method, both triangles and vertices will be loaded into the mesh, while all normals will be recalculated as well.
```
private void LoadMeshData(Mesh mesh)
{
    mesh.Clear();

    mesh.vertices = _grid.Vertices;
    mesh.triangles = _grid.Triangles;

    mesh.RecalculateNormals();
}
```

# Limitations

* Please be advised that Unity imposes a limit of 55,000 vertices per object, which equates to a maximum display of 14,400 quads.

* It is important to note that each quad is comprised of 6 angles or two triangles, and that Unity mesh triangles exclusively support integer values. Consequently, it is possible to reach an integer limit.
