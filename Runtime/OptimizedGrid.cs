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
        public int NodeCount => GridWidth > 0 && GridHeight > 0 ? GridWidth * GridHeight : 0;

        public List<Vector3> Vertices { get; private set; } = new();
        public List<int> Triangles { get; private set; } = new();
        public List<Color> Colors { get; private set; } = new();

        /// <summary>Per-node occupancy, indexed by <c>y * GridWidth + x</c>.</summary>
        public bool[] Occupied { get; private set; }

        /// <summary>Per-node color, indexed by <c>y * GridWidth + x</c>.</summary>
        public Color[] NodeColors { get; private set; }

        /// <summary>
        /// Creates a grid. If <paramref name="occupied"/> or <paramref name="nodeColors"/>
        /// are supplied with exactly <c>gridWidth * gridHeight</c> entries, the grid uses
        /// those arrays directly, so an owner can serialize them and hand them back to
        /// a rebuilt grid to keep node state across regeneration.
        /// </summary>
        public OptimizedGrid(int gridWidth, int gridHeight, float nodeWidth, float nodeHeight, float spacing,
            bool[] occupied = null, Color[] nodeColors = null)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            NodeWidth = nodeWidth;
            NodeHeight = nodeHeight;
            Spacing = spacing;

            int nodeCount = NodeCount;

            Occupied = occupied != null && occupied.Length == nodeCount
                ? occupied
                : new bool[nodeCount];

            if (nodeColors != null && nodeColors.Length == nodeCount)
            {
                NodeColors = nodeColors;
            }
            else
            {
                NodeColors = new Color[nodeCount];
                for (int i = 0; i < nodeCount; i++)
                {
                    NodeColors[i] = Color.white;
                }
            }
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        }

        public int GetNodeIndex(int x, int y)
        {
            return y * GridWidth + x;
        }

        public void SetNodeOccupied(int x, int y, bool occupied)
        {
            if (!IsInBounds(x, y)) return;

            Occupied[GetNodeIndex(x, y)] = occupied;
        }

        public bool IsNodeOccupied(int x, int y)
        {
            return IsInBounds(x, y) && Occupied[GetNodeIndex(x, y)];
        }

        public Color GetNodeColor(int x, int y)
        {
            return IsInBounds(x, y) ? NodeColors[GetNodeIndex(x, y)] : default;
        }

        public void SetNodeColor(int x, int y, Color color)
        {
            if (!IsInBounds(x, y)) return;

            int nodeIndex = GetNodeIndex(x, y);
            NodeColors[nodeIndex] = color;

            // Mirror into the vertex buffer if the mesh data has been generated.
            if (Colors.Count == NodeCount * VerticesPerNode)
            {
                int vertexIndex = nodeIndex * VerticesPerNode;
                for (int i = 0; i < VerticesPerNode; i++)
                {
                    Colors[vertexIndex + i] = color;
                }
            }
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

                    AddNode(xPos, yPos, vertexIndex, NodeColors[GetNodeIndex(x, y)], isHorizontal);
                    vertexIndex += VerticesPerNode;
                }
            }
        }

        private void AddNode(float xPos, float yPos, int vertexIndex, Color color, bool isHorizontal)
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

            Colors.AddRange(new[] { color, color, color, color });

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
    }
}
