using UnityEngine;

namespace TOMICZ.Grid.Examples
{
    /// <summary>Mouse-to-node picking shared by the example scripts.</summary>
    public static class GridMousePicker
    {
        /// <summary>
        /// Intersects the camera ray through <paramref name="screenPosition"/> with
        /// the grid's own plane, so no collider is needed. Returns false when the
        /// ray misses the plane or lands outside the grid.
        /// </summary>
        public static bool TryPick(GridGenerator grid, Camera camera, Vector3 screenPosition, out int x, out int y)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            Transform t = grid.transform;
            Vector3 normal = grid.IsHorizontal ? t.up : t.forward;
            var plane = new Plane(normal, t.position);

            if (!plane.Raycast(ray, out float distance))
            {
                x = y = -1;
                return false;
            }

            return grid.WorldToNode(ray.GetPoint(distance), out x, out y);
        }
    }
}
