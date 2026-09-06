<p align="center">
<img src="https://media.giphy.com/media/mdFObXCvTojzmWnQvf/giphy.gif" alt="Grid animation" title="Grid GIF" width="500"/>
</p>

# Procedural Grid

A procedurally generated grid for Unity, rendered as a single vertex-colored mesh. Every cell is four vertices in one mesh rather than a GameObject, so even a very large grid is one object and one draw call. Each node carries an occupancy flag and a color, which makes it a good base for pathfinding visualisation.

# Installation

In the Package Manager choose *Add package from git URL* and paste:

```
https://github.com/tomicz/unity-procedurally-generated-grid.git
```

Or copy the repository into your project's `Packages` folder. Requires Unity 2021.3 or newer.

# Getting started

## In the editor

Add the `GridGenerator` component to an empty GameObject. It adds the MeshFilter and MeshRenderer it needs, builds the mesh, and rebuilds it whenever you change a value in the inspector. Assign `Materials/GridMaterial` to its material field, or any material that reads vertex colors.

The included `Custom/VertexColor` shader is unlit and the mesh carries no normals. A lit material would need normals added.

Node colors and occupancy set through the component are serialized with the scene and survive regeneration. When you resize the grid, the overlapping region keeps its state and new nodes start empty and white.

```csharp
using TOMICZ.Grid;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private GridGenerator _grid;

    private void Start()
    {
        _grid.SetNodeOccupied(3, 4, true);
        _grid.SetNodeColor(3, 4, Color.red);

        bool blocked = _grid.IsNodeOccupied(3, 4);
    }
}
```

## From code

`OptimizedGrid` is a plain C# class with no scene dependencies. Use it directly when you want to drive your own mesh or run it outside a MonoBehaviour.

```csharp
using TOMICZ.Grid;

var grid = new OptimizedGrid(gridWidth: 50, gridHeight: 50, nodeWidth: 1f, nodeHeight: 1f, spacing: 0.1f);
grid.GenerateGrid(isHorizontal: true);   // XZ plane facing up; false for the XY plane
grid.LoadMeshData(mesh);                 // uploads vertices, triangles and colors

grid.SetNodeColor(10, 12, Color.green);
grid.LoadMeshColors(mesh);               // uploads only the color buffer
```

Nodes are indexed by `y * GridWidth + x`. `GetNodeIndex`, `IsInBounds`, `Occupied` and `NodeColors` are public for pathfinding code that wants raw array access.

## Coordinates

`NodeToWorld(x, y)` returns the world-space center of a node, and `WorldToNode(position, out x, out y)` maps a world position back to a node, returning false outside the grid. A position that lands in a spacing gap belongs to the node before the gap. The plain `OptimizedGrid` offers the same in local space as `GetNodeCenter` and `TryGetNode`.

# Running the tests

Edit-mode tests live under `Tests/Editor`. To see them in the Test Runner of a project that installs this package, list the package in that project's `Packages/manifest.json`:

```json
"testables": ["com.tomicz.procedural-grid"]
```

# Size

A node is four vertices and six indices, about 90 bytes of mesh data. The mesh switches to 32-bit indices automatically once it passes 65,535 vertices, so there is no hard cap on grid size beyond memory.

# License

MIT. See `LICENSE.md`.
