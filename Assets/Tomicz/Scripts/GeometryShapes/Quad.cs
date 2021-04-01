using UnityEngine;

namespace Tomicz.GeometryShapes
{
    [System.Serializable]
    public class Quad
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

        public int[] GetTriangle(int a0, int a1, int a2, int a3)
        {
            int[] triangles = new int[6]
            {
            0 + a0, 1 + a1, 2 + a2,
            2 + a2, 1 + a1, 3 + a3
            };

            return triangles;
        }

        public Vector3[] GetVerticesData()
        {
            Vector3[] vertices = new Vector3[4]
            {
            new Vector3(x, y, 0),
            new Vector3(x, y + height, 0),
            new Vector3(x + width, y, 0),
            new Vector3(x + width, y + height, 0),
            };

            return vertices;
        }
    }
}
