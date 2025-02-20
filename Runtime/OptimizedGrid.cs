using System.Collections.Generic;
using UnityEngine;

namespace TOMICZ.Grid
{
    public class OptimizedGrid
    {
        private const int _maxVerticesPerMesh = 65000;
        private const int _verticesPerQuad = 4;
        private const int _maxQuadsPerMesh = _maxVerticesPerMesh / _verticesPerQuad;

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float NodeWidth { get; private set; }
        public float NodeHeight { get; private set; }
        public float Spacing { get; private set; }

        public List<GridMeshSection> MeshSections { get; private set; } = new();
        private bool[,] _occupiedNodes;

        public class GridMeshSection
        {
            public List<Vector3> Vertices = new();
            public List<int> Triangles = new();
            public List<Color> Colors = new();
            public int StartX, StartY, Width, Height;
            public int VertexOffset;
        }

        private Color _defaultColor = Color.white;
        private bool _isHorizontal;

        public OptimizedGrid(int gridWidth, int gridHeight, float nodeWidth, float nodeHeight, float spacing)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            NodeWidth = nodeWidth;
            NodeHeight = nodeHeight;
            Spacing = spacing;
            _occupiedNodes = new bool[gridWidth, gridHeight];
        }

        public void SetNodeOccupied(int x, int y, bool occupied)
        {
            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
            {
                _occupiedNodes[x, y] = occupied;
            }
        }

        public bool IsNodeOccupied(int x, int y)
        {
            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
            {
                return _occupiedNodes[x, y];
            }
            return false;
        }

        public void GenerateGrid(bool isHorizontal = false)
        {
            _isHorizontal = isHorizontal;
            MeshSections.Clear();

            if (GridWidth <= 0 || GridHeight <= 0)
                return;

            int quadsPerSection = _maxQuadsPerMesh;
            int rowsPerSection = quadsPerSection / GridWidth;
            int sectionsNeeded = Mathf.CeilToInt((float)GridHeight / rowsPerSection);

            int vertexOffset = 0;
            for (int sectionIndex = 0; sectionIndex < sectionsNeeded; sectionIndex++)
            {
                int startY = sectionIndex * rowsPerSection;
                int sectionHeight = Mathf.Min(rowsPerSection, GridHeight - startY);

                if (sectionHeight <= 0) break;

                GridMeshSection section = new GridMeshSection
                {
                    StartX = 0,
                    StartY = startY,
                    Width = GridWidth,
                    Height = sectionHeight,
                    VertexOffset = vertexOffset
                };

                GenerateGridSection(section, isHorizontal);
                MeshSections.Add(section);

                vertexOffset += section.Width * section.Height * 4;
            }
        }

        private void GenerateGridSection(GridMeshSection section, bool isHorizontal)
        {
            float totalWidth = GridWidth * (NodeWidth + Spacing) - Spacing;
            float totalHeight = GridHeight * (NodeHeight + Spacing) - Spacing;
            float startX = -totalWidth / 2f;
            float startY = -totalHeight / 2f;

            int localVertexIndex = 0;

            for (int y = section.StartY; y < section.StartY + section.Height; y++)
            {
                for (int x = 0; x < section.Width; x++)
                {
                    float xPos = startX + x * (NodeWidth + Spacing);
                    float yPos = startY + y * (NodeHeight + Spacing);

                    AddNodeToSection(section, xPos, yPos, localVertexIndex, isHorizontal);
                    localVertexIndex += 4;
                }
            }
        }

        private void AddNodeToSection(GridMeshSection section, float xPos, float yPos, int localVertexIndex, bool isHorizontal)
        {
            if (isHorizontal)
            {
                section.Vertices.AddRange(new[]
                {
                    new Vector3(xPos, 0, yPos),
                    new Vector3(xPos + NodeWidth, 0, yPos),
                    new Vector3(xPos, 0, yPos + NodeHeight),
                    new Vector3(xPos + NodeWidth, 0, yPos + NodeHeight)
                });
            }
            else
            {
                section.Vertices.AddRange(new[]
                {
                    new Vector3(xPos, yPos, 0),
                    new Vector3(xPos + NodeWidth, yPos, 0),
                    new Vector3(xPos, yPos + NodeHeight, 0),
                    new Vector3(xPos + NodeWidth, yPos + NodeHeight, 0)
                });
            }

            section.Colors.AddRange(new[] { _defaultColor, _defaultColor, _defaultColor, _defaultColor });
            
            section.Triangles.AddRange(new[]
            {
                localVertexIndex, localVertexIndex + 2, localVertexIndex + 1,
                localVertexIndex + 2, localVertexIndex + 3, localVertexIndex + 1
            });
        }

        public void LoadMeshData(Mesh mesh)
        {
            if (mesh == null || MeshSections.Count == 0) return;

            GridMeshSection section = MeshSections[0];
            
            mesh.Clear();
            mesh.vertices = section.Vertices.ToArray();
            mesh.triangles = section.Triangles.ToArray();
            mesh.colors = section.Colors.ToArray();
            mesh.RecalculateNormals();
        }

        public void SetNodeColor(int x, int y, Color color)
        {
            foreach (GridMeshSection section in MeshSections)
            {
                if (y >= section.StartY && y < section.StartY + section.Height)
                {
                    int localY = y - section.StartY;
                    int vertexIndex = (localY * section.Width + x) * 4;
                    
                    for (int i = 0; i < 4; i++)
                    {
                        section.Colors[vertexIndex + i] = color;
                    }
                    break;
                }
            }
        }
    }
}
