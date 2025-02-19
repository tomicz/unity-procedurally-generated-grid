using System.Collections.Generic;
using TOMICZ.Grid.GeometryShapes;
using UnityEngine;

namespace TOMICZ.Grid
{
    public class OptimizedGrid
    {
        public int gridWidth;

        public int gridHeight;

        public float nodeWidth;

        public float nodeHeight;

        public float spacing;

        public Vector3[] Vertices => _verticesList.ToArray();

        public int[] Triangles => _trianglesList.ToArray();

        private const int AngleOffset = 4;

        private List<Vector3> _verticesList;

        private List<int> _trianglesList;

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
            _verticesList = new List<Vector3>();
            _trianglesList = new List<int>();

            int vertexIndex = 0;
            int triangleIndex = 0;

            float totalWidth = gridWidth * (nodeWidth + spacing) - spacing;
            float totalHeight = gridHeight * (nodeHeight + spacing) - spacing;
            float startX = -totalWidth / 2f;
            float startY = -totalHeight / 2f;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    float xPos = startX + x * (nodeWidth + spacing);
                    float yPos = startY + y * (nodeHeight + spacing);

                    if (isHorizontal)
                    {
                        _verticesList.Add(new Vector3(xPos, 0, yPos));
                        _verticesList.Add(new Vector3(xPos + nodeWidth, 0, yPos));
                        _verticesList.Add(new Vector3(xPos, 0, yPos + nodeHeight));
                        _verticesList.Add(new Vector3(xPos + nodeWidth, 0, yPos + nodeHeight));
                    }
                    else
                    {
                        _verticesList.Add(new Vector3(xPos, yPos, 0));
                        _verticesList.Add(new Vector3(xPos + nodeWidth, yPos, 0));
                        _verticesList.Add(new Vector3(xPos, yPos + nodeHeight, 0));
                        _verticesList.Add(new Vector3(xPos + nodeWidth, yPos + nodeHeight, 0));
                    }

                    _trianglesList.Add(vertexIndex);
                    _trianglesList.Add(vertexIndex + 2);
                    _trianglesList.Add(vertexIndex + 1);
                    _trianglesList.Add(vertexIndex + 2);
                    _trianglesList.Add(vertexIndex + 3);
                    _trianglesList.Add(vertexIndex + 1);

                    vertexIndex += 4;
                    triangleIndex += 6;
                }
            }
        }

        public void LoadMeshData(Mesh mesh)
        {
            if(mesh == null)
            {
                return;
            }

            mesh.Clear();

            mesh.vertices = _verticesList.ToArray();
            mesh.triangles = _trianglesList.ToArray();

            mesh.RecalculateNormals();
        }

        private void InitilizeDataContainers()
        {
            _trianglesList = new List<int>();
            _verticesList = new List<Vector3>();
        }

        private void SortQuadData(int index, Quad quad)
        {
            SortVerticies(quad);
            SortTriangles(index, quad);
        }

        private void SortVerticies(Quad quad) => _verticesList.AddRange(quad.GetVerticies());

        private void SortTriangles(int index, Quad quad) => _trianglesList.AddRange(quad.GetTriangles(AngleOffset * index));

        private Quad AddQuad(int x, int y) => new Quad(Position(x), Position(y), nodeWidth, nodeHeight);

        private float Position(float coord) => coord * (nodeWidth + spacing);
    }
}
