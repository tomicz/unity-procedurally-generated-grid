using UnityEngine;

namespace TOMICZ.Grid.Examples
{
    /// <summary>
    /// Click or drag on the grid to toggle nodes between free and occupied.
    /// Picks nodes by intersecting the mouse ray with the grid's own plane,
    /// so no collider is needed. Uses the legacy Input Manager.
    /// </summary>
    [RequireComponent(typeof(GridGenerator))]
    public class GridPainter : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Color32 _occupiedColor = new Color32(220, 60, 60, 255);
        [SerializeField] private Color32 _freeColor = new Color32(255, 255, 255, 255);

        private GridGenerator _grid;
        private int _lastX = -1;
        private int _lastY = -1;
        private bool _paintOccupied;

        private void Awake()
        {
            _grid = GetComponent<GridGenerator>();

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            if (_camera == null) return;

            if (Input.GetMouseButtonUp(0))
            {
                _lastX = _lastY = -1;
            }

            if (!Input.GetMouseButton(0)) return;
            if (!TryPickNode(out int x, out int y)) return;
            if (x == _lastX && y == _lastY) return;

            // The first node of a drag decides whether the drag paints or erases.
            if (_lastX < 0)
            {
                _paintOccupied = !_grid.IsNodeOccupied(x, y);
            }

            _grid.SetNodeOccupied(x, y, _paintOccupied);
            _grid.SetNodeColor(x, y, _paintOccupied ? _occupiedColor : _freeColor);

            _lastX = x;
            _lastY = y;
        }

        private bool TryPickNode(out int x, out int y)
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            Vector3 normal = _grid.IsHorizontal ? transform.up : transform.forward;
            var plane = new Plane(normal, transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                x = y = -1;
                return false;
            }

            return _grid.WorldToNode(ray.GetPoint(distance), out x, out y);
        }
    }
}
