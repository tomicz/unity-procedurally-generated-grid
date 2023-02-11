using UnityEngine;

namespace TOMICZ.Grid.GeometryShapes
{
    [System.Serializable]
    public struct Quad
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Quad(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public int[] GetTriangles(int nextAngleMultiplier)
        {
            int[] triangles = new int[6]
            {
                  0 + nextAngleMultiplier, 1 + nextAngleMultiplier, 2 + nextAngleMultiplier,
                  2 + nextAngleMultiplier, 1 + nextAngleMultiplier, 3 + nextAngleMultiplier
            };

            return triangles;
        }

        public Vector3[] GetVerticies()
        {
            return new Vector3[4]
            {
                new Vector3(x, y, 0),
                new Vector3(x, y + height, 0),
                new Vector3(x + width, y, 0),
                new Vector3(x + width, y + height, 0),
            };
        }
    }
}
