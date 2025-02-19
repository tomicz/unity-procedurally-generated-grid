using System.Collections.Generic;
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
        public Color[] Colors => _colorsList.ToArray();

        private List<Vector3> _verticesList;
        private List<int> _trianglesList;
        private List<Color> _colorsList;
        private Color _defaultColor = Color.white;

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
            InitializeLists();
            CalculateGridDimensions(out float startX, out float startY);
            GenerateNodes(startX, startY, isHorizontal);
        }

        private void InitializeLists()
        {
            _verticesList = new List<Vector3>();
            _trianglesList = new List<int>();
            _colorsList = new List<Color>();
        }

        private void CalculateGridDimensions(out float startX, out float startY)
        {
            float totalWidth = gridWidth * (nodeWidth + spacing) - spacing;
            float totalHeight = gridHeight * (nodeHeight + spacing) - spacing;
            startX = -totalWidth / 2f;
            startY = -totalHeight / 2f;
        }

        private void GenerateNodes(float startX, float startY, bool isHorizontal)
        {
            int vertexIndex = 0;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    float xPos = startX + x * (nodeWidth + spacing);
                    float yPos = startY + y * (nodeHeight + spacing);

                    AddNodeVertices(xPos, yPos, isHorizontal);
                    AddNodeColors();
                    AddNodeTriangles(vertexIndex);

                    vertexIndex += 4;
                }
            }
        }

        private void AddNodeVertices(float xPos, float yPos, bool isHorizontal)
        {
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
        }

        private void AddNodeColors()
        {
            for (int i = 0; i < 4; i++)
            {
                _colorsList.Add(_defaultColor);
            }
        }

        private void AddNodeTriangles(int vertexIndex)
        {
            _trianglesList.Add(vertexIndex);
            _trianglesList.Add(vertexIndex + 2);
            _trianglesList.Add(vertexIndex + 1);
            _trianglesList.Add(vertexIndex + 2);
            _trianglesList.Add(vertexIndex + 3);
            _trianglesList.Add(vertexIndex + 1);
        }

        public void SetNodeColor(int x, int y, Color color)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                return;

            int vertexIndex = (y * gridWidth + x) * 4;
            
            for (int i = 0; i < 4; i++)
            {
                _colorsList[vertexIndex + i] = color;
            }
        }

        public void LoadMeshData(Mesh mesh)
        {
            if(mesh == null)
                return;

            mesh.Clear();
            mesh.vertices = _verticesList.ToArray();
            mesh.triangles = _trianglesList.ToArray();
            mesh.colors = _colorsList.ToArray();
            mesh.RecalculateNormals();
        }
    }
}
