using UnityEngine;
using UnityEngine.Rendering;

namespace TOMICZ.Grid
{
    public class OptimizedGrid
    {
        public const int VerticesPerNode = 4;
        public const int IndicesPerNode = 6;

        /// <summary>Largest number of entries GetNeighbors can write.</summary>
        public const int MaxNeighbors = 8;

        /// <summary>Default node color. Color32 is 4 bytes per vertex where Color is 16.</summary>
        public static readonly Color32 White = new Color32(255, 255, 255, 255);

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float NodeWidth { get; private set; }
        public float NodeHeight { get; private set; }
        public float Spacing { get; private set; }
        public bool IsHorizontal { get; private set; }
        public int NodeCount => GridWidth > 0 && GridHeight > 0 ? GridWidth * GridHeight : 0;

        /// <summary>Distance from one node's origin to the next along each axis.</summary>
        public float StepX => NodeWidth + Spacing;
        public float StepY => NodeHeight + Spacing;

        /// <summary>Total extent of the grid, not counting the spacing after the last node.</summary>
        public float TotalWidth => GridWidth > 0 ? GridWidth * StepX - Spacing : 0f;
        public float TotalHeight => GridHeight > 0 ? GridHeight * StepY - Spacing : 0f;

        /// <summary>Local-space corner of node (0, 0). The grid is centered on the origin.</summary>
        public float OriginX => -TotalWidth * 0.5f;
        public float OriginY => -TotalHeight * 0.5f;

        public Vector3[] Vertices { get; private set; } = System.Array.Empty<Vector3>();
        public int[] Triangles { get; private set; } = System.Array.Empty<int>();
        public Color32[] Colors { get; private set; } = System.Array.Empty<Color32>();

        /// <summary>Per-node occupancy, indexed by <c>y * GridWidth + x</c>.</summary>
        public bool[] Occupied { get; private set; }

        /// <summary>Per-node color, indexed by <c>y * GridWidth + x</c>.</summary>
        public Color32[] NodeColors { get; private set; }

        /// <summary>
        /// True when node colors have changed since they were last uploaded with
        /// LoadMeshData or LoadMeshColors. Lets callers batch many SetNodeColor
        /// calls into a single upload.
        /// </summary>
        public bool ColorsDirty { get; private set; }

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

        /// <summary>Inverse of GetNodeIndex. Not bounds-checked.</summary>
        public void GetNodeCoordinates(int nodeIndex, out int x, out int y)
        {
            x = nodeIndex % GridWidth;
            y = nodeIndex / GridWidth;
        }

        /// <summary>
        /// Local-space center of a node, in the XZ plane for horizontal grids and XY
        /// otherwise. Not bounds-checked, so positions just outside the grid can be queried.
        /// </summary>
        public Vector3 GetNodeCenter(int x, int y)
        {
            float cx = OriginX + x * StepX + NodeWidth * 0.5f;
            float cy = OriginY + y * StepY + NodeHeight * 0.5f;
            return IsHorizontal ? new Vector3(cx, 0f, cy) : new Vector3(cx, cy, 0f);
        }

        /// <summary>
        /// Maps a local-space position to node coordinates. Returns false outside the
        /// grid. A position inside a spacing gap belongs to the node before the gap.
        /// </summary>
        public bool TryGetNode(Vector3 localPosition, out int x, out int y)
        {
            if (StepX <= 0f || StepY <= 0f)
            {
                x = y = -1;
                return false;
            }

            float v = IsHorizontal ? localPosition.z : localPosition.y;
            x = Mathf.FloorToInt((localPosition.x - OriginX) / StepX);
            y = Mathf.FloorToInt((v - OriginY) / StepY);
            return IsInBounds(x, y);
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

        /// <summary>Marks every node as free.</summary>
        public void ClearOccupancy()
        {
            System.Array.Clear(Occupied, 0, Occupied.Length);
        }

        /// <summary>
        /// Writes the node indices of the free neighbours of (x, y) into
        /// <paramref name="results"/> and returns how many were written. Never
        /// allocates; the buffer needs room for MaxNeighbors entries. A diagonal is
        /// only offered when both orthogonal nodes beside it are free, so paths
        /// cannot cut corners through obstacles. Pass skipOccupied false to get
        /// every in-bounds neighbour regardless of occupancy.
        /// </summary>
        public int GetNeighbors(int x, int y, int[] results, bool includeDiagonals = false, bool skipOccupied = true)
        {
            int count = 0;
            bool left = TryAddNeighbor(x - 1, y, results, ref count, skipOccupied);
            bool right = TryAddNeighbor(x + 1, y, results, ref count, skipOccupied);
            bool down = TryAddNeighbor(x, y - 1, results, ref count, skipOccupied);
            bool up = TryAddNeighbor(x, y + 1, results, ref count, skipOccupied);

            if (includeDiagonals)
            {
                if (left && down) TryAddNeighbor(x - 1, y - 1, results, ref count, skipOccupied);
                if (right && down) TryAddNeighbor(x + 1, y - 1, results, ref count, skipOccupied);
                if (left && up) TryAddNeighbor(x - 1, y + 1, results, ref count, skipOccupied);
                if (right && up) TryAddNeighbor(x + 1, y + 1, results, ref count, skipOccupied);
            }

            return count;
        }

        private bool TryAddNeighbor(int x, int y, int[] results, ref int count, bool skipOccupied)
        {
            if (!IsInBounds(x, y)) return false;

            int index = GetNodeIndex(x, y);
            if (skipOccupied && Occupied[index]) return false;

            results[count++] = index;
            return true;
        }

        public Color32 GetNodeColor(int x, int y)
        {
            return IsInBounds(x, y) ? NodeColors[GetNodeIndex(x, y)] : default;
        }

        public void SetNodeColor(int x, int y, Color32 color)
        {
            if (!IsInBounds(x, y)) return;

            SetNodeColor(GetNodeIndex(x, y), color);
        }

        /// <summary>Index-based overload for callers that already hold a node index. Not bounds-checked.</summary>
        public void SetNodeColor(int nodeIndex, Color32 color)
        {
            NodeColors[nodeIndex] = color;
            ColorsDirty = true;

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

        /// <summary>Sets every node to one color. Useful to reset a visualisation before repainting.</summary>
        public void SetAllNodeColors(Color32 color)
        {
            for (int i = 0; i < NodeColors.Length; i++)
            {
                NodeColors[i] = color;
            }

            for (int i = 0; i < Colors.Length; i++)
            {
                Colors[i] = color;
            }

            ColorsDirty = true;
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

            float stepX = StepX;
            float stepY = StepY;
            float startX = OriginX;
            float startY = OriginY;

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
            ColorsDirty = false;

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
            ColorsDirty = false;
        }
    }
}
