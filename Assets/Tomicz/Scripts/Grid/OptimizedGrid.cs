using System.Collections.Generic;
using Tomicz.GeometryShapes;
using UnityEngine;

namespace Tomicz.Grid
{
    public class OptimizedGrid
    {
        #region Public Fields

        public int gridWidth;

        public int gridHeight;

        public float nodeWidth;

        public float nodeHeight;

        public float spacing;

        /// <summary>
        /// Get all procedural quads.
        /// </summary>
        public Quad[,] Quads => _quadArray;

        /// <summary>
        /// Get all vretices data in the grid.
        /// </summary>
        public Vector3[] Vertices => _verticesList.ToArray();

        /// <summary>
        /// Get all triangles data in the grid.
        /// </summary>
        public int[] Triangles => _trianglesList.ToArray();

        public Node[,] Nodes => _nodeList;

        #endregion

        #region Private Fields

        // Private fields

        private List<Vector3> _verticesList;

        private Quad[,] _quadArray;

        private const int AngleOffset = 4;

        private List<int> _trianglesList;

        private Node[,] _nodeList;

        #endregion

        /// <summary>
        /// Creates an instance of a grid.
        /// </summary>
        /// <param name="gridWidth">Max grid width.</param>
        /// <param name="gridHeight">Max grid height.</param>
        /// <param name="nodeWidth">Single node width.</param>
        /// <param name="nodeHeight">Single node height</param>
        /// <param name="spacing">Spacing between nodes.</param>
        public OptimizedGrid(int gridWidth, int gridHeight, float nodeWidth, float nodeHeight, float spacing)
        {
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.nodeWidth = nodeWidth;
            this.nodeHeight = nodeHeight;
            this.spacing = spacing;
        }

        /// <summary>
        /// Generates a new grid when called.
        /// </summary>
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

        /// <summary>
        /// Initilizes data containers.
        /// </summary>
        private void InitilizeDataContainers()
        {
            _quadArray = new Quad[gridWidth, gridHeight];
            _trianglesList = new List<int>();
            _verticesList = new List<Vector3>();
            _nodeList = new Node[gridWidth, gridHeight];
        }

        /// <summary>
        /// Stores triangles and vertices insade a list.
        /// </summary>
        /// <param name="index">Quad index.</param>
        /// <param name="quad">Quad object.</param>
        private void SortQuadData(int index, Quad quad)
        {
            var newQuadTriangle = AngleOffset * index;

            _verticesList.AddRange(quad.GetVerticesData());
            _trianglesList.AddRange(quad.GetTriangle(newQuadTriangle, newQuadTriangle, newQuadTriangle, newQuadTriangle));
        }


        /// <summary>
        /// Create a new quad shape.
        /// </summary>
        /// <param name="x">Quad position insde a 2D array list on X axis</param>
        /// <param name="y">Quad position insde a 2D array list on Y axis</param>
        /// <returns></returns>
        private Quad AddQuad(int x, int y)
        {
            var positionX = x * (nodeWidth + spacing);
            var positionY = y * (nodeHeight + spacing);

            Quad quad = new Quad(positionX, positionY, nodeWidth, nodeHeight);
            _quadArray[x, y] = quad;
            return quad;
        }


        /// <summary>
        /// Add a new node to nodes list. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void AddNode(int x, int y) => _nodeList[x, y] = new Node(x, y);
    }
}
