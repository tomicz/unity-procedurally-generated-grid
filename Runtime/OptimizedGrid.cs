using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TOMICZ.Grid
{
    public class OptimizedGrid
    {
        private const int VerticesPerNode = 4;

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float NodeWidth { get; private set; }
        public float NodeHeight { get; private set; }
        public float Spacing { get; private set; }
        public bool IsHorizontal { get; private set; }

        public List<Vector3> Vertices { get; private set; } = new();
        public List<int> Triangles { get; private set; } = new();
        public List<Color> Colors { get; private set; } = new();

        private bool[,] _occupiedNodes;
        private Color _defaultColor = Color.white;

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
            IsHorizontal = isHorizontal;
            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();

            if (GridWidth <= 0 || GridHeight <= 0)
                return;

            float totalWidth = GridWidth * (NodeWidth + Spacing) - Spacing;
            float totalHeight = GridHeight * (NodeHeight + Spacing) - Spacing;
            float startX = -totalWidth / 2f;
            float startY = -totalHeight / 2f;

            int vertexIndex = 0;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    float xPos = startX + x * (NodeWidth + Spacing);
                    float yPos = startY + y * (NodeHeight + Spacing);

                    AddNode(xPos, yPos, vertexIndex, isHorizontal);
                    vertexIndex += VerticesPerNode;
                }
            }
        }

        private void AddNode(float xPos, float yPos, int vertexIndex, bool isHorizontal)
        {
            if (isHorizontal)
            {
                Vertices.AddRange(new[]
                {
                    new Vector3(xPos, 0, yPos),
                    new Vector3(xPos + NodeWidth, 0, yPos),
                    new Vector3(xPos, 0, yPos + NodeHeight),
                    new Vector3(xPos + NodeWidth, 0, yPos + NodeHeight)
                });
            }
            else
            {
                Vertices.AddRange(new[]
                {
                    new Vector3(xPos, yPos, 0),
                    new Vector3(xPos + NodeWidth, yPos, 0),
                    new Vector3(xPos, yPos + NodeHeight, 0),
                    new Vector3(xPos + NodeWidth, yPos + NodeHeight, 0)
                });
            }

            Colors.AddRange(new[] { _defaultColor, _defaultColor, _defaultColor, _defaultColor });

            Triangles.AddRange(new[]
            {
                vertexIndex, vertexIndex + 2, vertexIndex + 1,
                vertexIndex + 2, vertexIndex + 3, vertexIndex + 1
            });
        }

        public void LoadMeshData(Mesh mesh)
        {
            if (mesh == null) return;

            mesh.Clear();
            // 16-bit indices cap a mesh at 65,535 vertices. Switching to 32-bit
            // lifts that to ~4 billion, so a single mesh can hold any grid size.
            mesh.indexFormat = Vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = Vertices.ToArray();
            mesh.triangles = Triangles.ToArray();
            mesh.colors = Colors.ToArray();
            mesh.RecalculateNormals();
        }

        public void SetNodeColor(int x, int y, Color color)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                return;

            int vertexIndex = (y * GridWidth + x) * VerticesPerNode;

            for (int i = 0; i < VerticesPerNode; i++)
            {
                Colors[vertexIndex + i] = color;
            }
        }
    }
}
