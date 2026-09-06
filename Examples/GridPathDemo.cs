using System.Collections.Generic;
using UnityEngine;

namespace TOMICZ.Grid.Examples
{
    /// <summary>
    /// Right-click once to place the start and again to place the goal. The path
    /// is found with GridPathfinder and redrawn whenever obstacles are painted
    /// with GridPainter. Uses the legacy Input Manager.
    /// </summary>
    [RequireComponent(typeof(GridGenerator))]
    public class GridPathDemo : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _allowDiagonals = true;
        [SerializeField] private Color32 _freeColor = new Color32(255, 255, 255, 255);
        [SerializeField] private Color32 _obstacleColor = new Color32(220, 60, 60, 255);
        [SerializeField] private Color32 _visitedColor = new Color32(205, 205, 205, 255);
        [SerializeField] private Color32 _pathColor = new Color32(255, 200, 40, 255);
        [SerializeField] private Color32 _startColor = new Color32(60, 200, 80, 255);
        [SerializeField] private Color32 _endColor = new Color32(60, 120, 255, 255);

        private GridGenerator _grid;
        private GridPathfinder _pathfinder;
        private readonly List<int> _path = new();
        private int _start = -1;
        private int _end = -1;
        private bool _dirty;

        /// <summary>Node indices of the current path, start to goal. Empty when there is none.</summary>
        public IReadOnlyList<int> Path => _path;

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

            if (Input.GetMouseButtonDown(1) && GridMousePicker.TryPick(_grid, _camera, Input.mousePosition, out int x, out int y))
            {
                int node = _grid.Grid.GetNodeIndex(x, y);

                if (_start < 0 || _end >= 0)
                {
                    _start = node;
                    _end = -1;
                }
                else
                {
                    _end = node;
                }

                _dirty = true;
            }

            // Obstacles may have changed while the left button was held (see GridPainter).
            if (Input.GetMouseButtonUp(0))
            {
                _dirty = true;
            }

            if (_dirty)
            {
                Repaint();
                _dirty = false;
            }
        }

        private void Repaint()
        {
            OptimizedGrid grid = _grid.Grid;
            if (grid == null) return;

            if (_pathfinder == null || _pathfinder.Grid != grid)
            {
                _pathfinder = new GridPathfinder(grid, _allowDiagonals);
            }
            _pathfinder.AllowDiagonals = _allowDiagonals;

            // The grid may have been regenerated smaller since the markers were placed.
            if (_start >= grid.NodeCount || _end >= grid.NodeCount)
            {
                _start = _end = -1;
            }

            bool searched = _start >= 0 && _end >= 0;
            if (searched)
            {
                grid.GetNodeCoordinates(_start, out int sx, out int sy);
                grid.GetNodeCoordinates(_end, out int ex, out int ey);
                _pathfinder.FindPath(sx, sy, ex, ey, _path);
            }
            else
            {
                _path.Clear();
            }

            // Every SetNodeColor below lands in one upload at the end of the frame.
            for (int i = 0; i < grid.NodeCount; i++)
            {
                Color32 color = grid.Occupied[i] ? _obstacleColor
                    : searched && _pathfinder.WasVisited(i) ? _visitedColor
                    : _freeColor;
                _grid.SetNodeColor(i, color);
            }

            foreach (int node in _path)
            {
                _grid.SetNodeColor(node, _pathColor);
            }

            if (_start >= 0) _grid.SetNodeColor(_start, _startColor);
            if (_end >= 0) _grid.SetNodeColor(_end, _endColor);
        }
    }
}
