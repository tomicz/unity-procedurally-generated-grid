using UnityEngine;
using UnityEngine.Rendering;

namespace TOMICZ.Grid
{
    public class OptimizedGrid
    {
        public const int VerticesPerNode = 4;
        public const int IndicesPerNode = 6;

        /// <summary>Default node color. Color32 is 4 bytes per vertex where Color is 16.</summary>
        public static readonly Color32 White = new Color32(255, 255, 255, 255);

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float NodeWidth { get; private set; }
        public float NodeHeight { get; private set; }
        public float Spacing { get; private set; }
        public bool IsHorizontal { get; private set; }
        public int NodeCount => GridWidth > 0 && GridHeight > 0 ? GridWidth * GridHeight : 0;

        public Vector3[] Vertices { get; private set; } = System.Array.Empty<Vector3>();
        public int[] Triangles { get; private set; } = System.Array.Empty<int>();
        public Color32[] Colors { get; private set; } = System.Array.Empty<Color32>();

        /// <summary>Per-node occupancy, indexed by <c>y * GridWidth + x</c>.</summary>
        public bool[] Occupied { get; private set; }

        /// <summary>Per-node color, indexed by <c>y * GridWidth + x</c>.</summary>
        public Color32[] NodeColors { get; private set; }

        /// <summary>
        /// Creates a grid. If <paramref name="occupied"/> or <paramref name="nodeColors"/>
        /// are supplied with exactly <c>gridWidth * gridHeight</c> entries, the grid uses
        /// those arrays directly, so an owner can serialize them and hand them back to
        /// a rebuilt grid to keep node state across regeneration.
        /// </summary>
        public OptimizedGrid(int gridWidth, int gridHeight, float nodeWidth, float nodeHeight, float spacing,
            bool[] occupied = null, Color32[] nodeColors = null)
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
                NodeColors = new Color32[nodeCount];
                for (int i = 0; i < nodeCount; i++)
                {
                    NodeColors[i] = White;
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

        public Color32 GetNodeColor(int x, int y)
        {
            return IsInBounds(x, y) ? NodeColors[GetNodeIndex(x, y)] : default;
        }

        public void SetNodeColor(int x, int y, Color32 color)
        {
            if (!IsInBounds(x, y)) return;

            int nodeIndex = GetNodeIndex(x, y);
            NodeColors[nodeIndex] = color;

            // Mirror into the vertex buffer if the mesh data has been generated.
            if (Colors.Length == NodeCount * VerticesPerNode)
            {
                int v = nodeIndex * VerticesPerNode;
                Colors[v] = color;
                Colors[v + 1] = color;
                Colors[v + 2] = color;
                Colors[v + 3] = color;
            }
        }

        public void GenerateGrid(bool isHorizontal = false)
        {
            IsHorizontal = isHorizontal;

            // Every size is known up front, so allocate each buffer exactly once
            // and write by index instead of growing lists with temporary arrays.
            int nodeCount = NodeCount;
            Vertices = new Vector3[nodeCount * VerticesPerNode];
            Triangles = new int[nodeCount * IndicesPerNode];
            Colors = new Color32[nodeCount * VerticesPerNode];

            if (nodeCount == 0)
                return;

            float stepX = NodeWidth + Spacing;
            float stepY = NodeHeight + Spacing;
            float startX = -(GridWidth * stepX - Spacing) / 2f;
            float startY = -(GridHeight * stepY - Spacing) / 2f;

            for (int y = 0; y < GridHeight; y++)
            {
                float yPos = startY + y * stepY;

                for (int x = 0; x < GridWidth; x++)
                {
                    float xPos = startX + x * stepX;
                    int nodeIndex = y * GridWidth + x;
                    int v = nodeIndex * VerticesPerNode;
                    int t = nodeIndex * IndicesPerNode;

                    if (isHorizontal)
                    {
                        Vertices[v] = new Vector3(xPos, 0, yPos);
                        Vertices[v + 1] = new Vector3(xPos + NodeWidth, 0, yPos);
                        Vertices[v + 2] = new Vector3(xPos, 0, yPos + NodeHeight);
                        Vertices[v + 3] = new Vector3(xPos + NodeWidth, 0, yPos + NodeHeight);
                    }
                    else
                    {
                        Vertices[v] = new Vector3(xPos, yPos, 0);
                        Vertices[v + 1] = new Vector3(xPos + NodeWidth, yPos, 0);
                        Vertices[v + 2] = new Vector3(xPos, yPos + NodeHeight, 0);
                        Vertices[v + 3] = new Vector3(xPos + NodeWidth, yPos + NodeHeight, 0);
                    }

                    Color32 color = NodeColors[nodeIndex];
                    Colors[v] = color;
                    Colors[v + 1] = color;
                    Colors[v + 2] = color;
                    Colors[v + 3] = color;

                    Triangles[t] = v;
                    Triangles[t + 1] = v + 2;
                    Triangles[t + 2] = v + 1;
                    Triangles[t + 3] = v + 2;
                    Triangles[t + 4] = v + 3;
                    Triangles[t + 5] = v + 1;
                }
            }
        }

        public void LoadMeshData(Mesh mesh)
        {
            if (mesh == null) return;

            mesh.Clear();

            if (Vertices.Length == 0)
                return;

            // 16-bit indices cap a mesh at 65,535 vertices. Switching to 32-bit
            // lifts that to ~4 billion, so a single mesh can hold any grid size.
            mesh.indexFormat = Vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(Vertices);
            mesh.SetTriangles(Triangles, 0);
            mesh.SetColors(Colors);
            // No normals: the vertex-color shader never reads them, and a flat
            // grid would only ever have one constant normal anyway.
        }

        /// <summary>
        /// Uploads only the vertex colors. Call after SetNodeColor instead of a full
        /// LoadMeshData, which would re-upload geometry that has not changed.
        /// </summary>
        public void LoadMeshColors(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount != Colors.Length) return;

            mesh.SetColors(Colors);
        }
    }
}
