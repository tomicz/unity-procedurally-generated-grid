# About

Optimized grid is a procedural grid and it is a great alternative to Unity's grid made of gameObjects. Most beginners don't understand how memory works in Unity and they will make bad decisions by following bad tutorials with bad programming practices. With this tool you will not need to manage memory and object pooling. Grid's nodes will not exist in memory while inactive. 

# How to use

`
    using Tomicz.Grid;
		
		private Mesh _mesh;
		private OptimizedGrid _grid;
		
    private void Start()
    {
				// Make sure you have MeshFilter and MeshRnderer attached to this gameObject.
				_mesh = GetComponent<MeshFilter>().mesh = _mesh;
				
        // Create an instance of a grid
        _grid = new OptimizedGrid(gridWidth, gridHeight, nodeWidth, nodeHeight, spacing);
    
        // This created necessary data when instance created.
				// Always clal after the instance.
				_grid.GenerateGrid();
				
				LoadData(_mesh);
    }
		
		private void LoadData(Mesh mesh)
		{
				mesh.vertices = _grid.Vertices;
        mesh.triangles = _grid.Triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
		}
`
