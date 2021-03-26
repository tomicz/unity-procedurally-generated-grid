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

        /// <summary>
        /// Generate a new mesh of quad shape.
        /// </summary>
        /// <param name="x">Position x.</param>
        /// <param name="y">Position y.</param>
        /// <param name="width">Shape width</param>
        /// <param name="height">Shape height</param>
        public Quad(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Returns triangles.
        /// </summary>
        /// <param name="a0">Angle 0</param>
        /// <param name="a1">Angle 1</param>
        /// <param name="a2">Angle 2</param>
        /// <param name="a3">Angle 3</param>
        /// <returns></returns>
        public int[] GetTriangle(int a0, int a1, int a2, int a3)
        {
            int[] triangles = new int[6]
            {
            0 + a0, 1 + a1, 2 + a2,
            2 + a2, 1 + a1, 3 + a3
            };

            return triangles;
        }

        /// <summary>
        /// Returns vertices in vectors.
        /// </summary>
        /// <returns></returns>
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
