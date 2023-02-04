
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

Add reference depenencies. Make sure you have MeshFilter and MeshRenderer attached to the gameObject.
```
private Mesh _mesh;
private OptimizedGrid _grid;
```
You can also use this inside an update with [ExecuteInEditMode]. If your application or game needs the grid to be updated in real time, then it's the best to update it with sliders or some other input. 
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

This method will load triangles and vertices into the mesh. Also, it will recalculate all the normals.
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

* Unity only allows 55k vertices per object, which means that you will be able to only display up to 14400 quads.

* Each quad contains 6 angles or two triangles and it's possible to reach an integer limit. Unity mesh triangles only support integer numbers. 
