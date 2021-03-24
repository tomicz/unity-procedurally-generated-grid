using System.Collections.Generic;
using Tomicz.GeometryShapes;
using UnityEngine;

namespace Tomicz.Grid
{
    public class OptimizedGrid
    {
        // Public fields

        public int gridWidth;

        public int gridHeight;

        public float nodeWidth;

        public float nodeHeight;

        public float spacing;

        /// <summary>
        /// Get all nodes
        /// </summary>
        public Quad[,] Nodes => _quadArray;

        /// <summary>
        /// Get all vretices data.
        /// </summary>
        public Vector3[] Vertices => _verticesList.ToArray();

        /// <summary>
        /// Get all triangles data
        /// </summary>
        public int[] Triangles => _trianglesList.ToArray();


        // Private fields

        private List<Vector3> _verticesList;

        private Quad[,] _quadArray;

        private const int AngleOffset = 4;

        private List<int> _trianglesList;

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
                    AddQuadData(i, CreateNewQuad(x, y));
                }
            }
        }

        /// <summary>
        /// Stores triangles and vertices to a list
        /// </summary>
        /// <param name="index"></param>
        /// <param name="quad"></param>
        private void AddQuadData(int index, Quad quad)
        {
            _verticesList.AddRange(quad.GetVerticesData());
            _trianglesList.AddRange(quad.GetTriangle(AngleOffset * index, AngleOffset * index, AngleOffset * index, AngleOffset * index));
        }

        /// <summary>
        /// Initilizes data containers.
        /// </summary>
        private void InitilizeDataContainers()
        {
            _quadArray = new Quad[gridWidth, gridHeight];
            _trianglesList = new List<int>();
            _verticesList = new List<Vector3>();
        }


        /// <summary>
        /// Create a new quad shape.
        /// </summary>
        /// <param name="x">2D Array position X</param>
        /// <param name="y">2D Array position y</param>
        /// <returns></returns>
        private Quad CreateNewQuad(int x, int y)
        {
            Quad quad = new Quad(x * (nodeWidth + spacing), y * (nodeHeight + spacing), nodeWidth, nodeHeight);
            _quadArray[x, y] = quad;
            return quad;
        }
    }
}
