using System.Collections.Generic;
using Tomicz.GeometryShapes;
using UnityEngine;

namespace Tomicz.Grid
{
    public class OptimizedGrid
    {
        public int gridWidth;

        public int gridHeight;

        public float nodeWidth;

        public float nodeHeight;

        public float spacing;

        public Quad[,] Quads => _quadArray;

        public Vector3[] Vertices => _verticesList.ToArray();

        public int[] Triangles => _trianglesList.ToArray();

        public Node[,] Nodes => _nodeList;

        private List<Vector3> _verticesList;

        private Quad[,] _quadArray;

        private const int AngleOffset = 4;

        private List<int> _trianglesList;

        private Node[,] _nodeList;

        public OptimizedGrid(int gridWidth, int gridHeight, float nodeWidth, float nodeHeight, float spacing)
        {
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.nodeWidth = nodeWidth;
            this.nodeHeight = nodeHeight;
            this.spacing = spacing;
        }

        public void GenerateGrid()
        {
            InitilizeDataContainers();

            for (int x = 0, i = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++, i++)
                {
                    SortQuadData(i, AddQuad(x, y));
                    AddNode(x, y);
                }
            }
        }

        private void InitilizeDataContainers()
        {
            _quadArray = new Quad[gridWidth, gridHeight];
            _trianglesList = new List<int>();
            _verticesList = new List<Vector3>();
            _nodeList = new Node[gridWidth, gridHeight];
        }

        private void SortQuadData(int index, Quad quad)
        {
            var newQuadTriangle = AngleOffset * index;

            _verticesList.AddRange(quad.GetVerticesData());
            _trianglesList.AddRange(quad.GetTriangle(newQuadTriangle, newQuadTriangle, newQuadTriangle, newQuadTriangle));
        }

        private Quad AddQuad(int x, int y)
        {
            var nodeX = x * (nodeWidth + spacing);
            var nodeY = y * (nodeHeight + spacing);

            Quad quad = new Quad(nodeX, nodeY, nodeWidth, nodeHeight);
            _quadArray[x, y] = quad;
            return quad;
        }

        private void AddNode(int x, int y) => _nodeList[x, y] = new Node(x, y);
    }
}
