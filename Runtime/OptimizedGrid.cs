using System.Collections.Generic;
using UnityEngine;

namespace TOMICZ.Grid
{
    public class OptimizedGrid
    {
        private const int MaxVerticesPerMesh = 65000;
        private const int VerticesPerQuad = 4;
        private const int MaxQuadsPerMesh = MaxVerticesPerMesh / VerticesPerQuad;

        public List<GridMeshSection> MeshSections { get; private set; } = new();

        public class GridMeshSection
        {
            public List<Vector3> Vertices = new();
            public List<int> Triangles = new();
            public List<Color> Colors = new();
            public int StartX, StartY, Width, Height;
            public int VertexOffset;  // Track vertex index offset for this section
        }

        public int gridWidth;
        public int gridHeight;
        public float nodeWidth;
        public float nodeHeight;
        public float spacing;

        private List<Vector3> _verticesList;
        private List<int> _trianglesList;
        private List<Color> _colorsList;
        private Color _defaultColor = Color.white;
        private bool _isHorizontal;

        public OptimizedGrid(int gridWidth, int gridHeight, float nodeWidth, float nodeHeight, float spacing)
        {
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.nodeWidth = nodeWidth;
            this.nodeHeight = nodeHeight;
            this.spacing = spacing;
        }

        public void GenerateGrid(bool isHorizontal = false)
        {
            _isHorizontal = isHorizontal;
            MeshSections.Clear();

            if (gridWidth <= 0 || gridHeight <= 0)
                return;

            int quadsPerSection = MaxQuadsPerMesh;
            int rowsPerSection = quadsPerSection / gridWidth;
            int sectionsNeeded = Mathf.CeilToInt((float)gridHeight / rowsPerSection);

            int vertexOffset = 0;
            for (int i = 0; i < sectionsNeeded; i++)
            {
                int startY = i * rowsPerSection;
                int height = Mathf.Min(rowsPerSection, gridHeight - startY);

                if (height <= 0) break;

                var section = new GridMeshSection
                {
                    StartX = 0,
                    StartY = startY,
                    Width = gridWidth,
                    Height = height,
                    VertexOffset = vertexOffset
                };

                GenerateGridSection(section, isHorizontal);
                MeshSections.Add(section);

                vertexOffset += section.Width * section.Height * 4;
            }
        }

        private void GenerateGridSection(GridMeshSection section, bool isHorizontal)
        {
            float totalWidth = gridWidth * (nodeWidth + spacing) - spacing;
            float totalHeight = gridHeight * (nodeHeight + spacing) - spacing;
            float startX = -totalWidth / 2f;
            float startY = -totalHeight / 2f;

            int localVertexIndex = 0;

            for (int y = section.StartY; y < section.StartY + section.Height; y++)
            {
                for (int x = 0; x < section.Width; x++)
                {
                    float xPos = startX + x * (nodeWidth + spacing);
                    float yPos = startY + y * (nodeHeight + spacing);

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
                    new Vector3(xPos + nodeWidth, 0, yPos),
                    new Vector3(xPos, 0, yPos + nodeHeight),
                    new Vector3(xPos + nodeWidth, 0, yPos + nodeHeight)
                });
            }
            else
            {
                section.Vertices.AddRange(new[]
                {
                    new Vector3(xPos, yPos, 0),
                    new Vector3(xPos + nodeWidth, yPos, 0),
                    new Vector3(xPos, yPos + nodeHeight, 0),
                    new Vector3(xPos + nodeWidth, yPos + nodeHeight, 0)
                });
            }

            section.Colors.AddRange(new[] { _defaultColor, _defaultColor, _defaultColor, _defaultColor });
            
            int vertexIndex = localVertexIndex;
            section.Triangles.AddRange(new[]
            {
                vertexIndex, vertexIndex + 2, vertexIndex + 1,
                vertexIndex + 2, vertexIndex + 3, vertexIndex + 1
            });
        }

        public void LoadMeshData(Mesh mesh)
        {
            if (mesh == null || MeshSections.Count == 0) return;

            // Get the first section
            var section = MeshSections[0];
            
            mesh.Clear();
            mesh.vertices = section.Vertices.ToArray();
            mesh.triangles = section.Triangles.ToArray();
            mesh.colors = section.Colors.ToArray();
            mesh.RecalculateNormals();
        }

        public void SetNodeColor(int x, int y, Color color)
        {
            foreach (var section in MeshSections)
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
