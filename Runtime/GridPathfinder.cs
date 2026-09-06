using System;
using System.Collections.Generic;

namespace TOMICZ.Grid
{
    /// <summary>
    /// A* over an OptimizedGrid's occupancy, with a binary heap for the open set.
    /// Buffers are sized to the grid on first use and reused, so repeated searches
    /// on the same grid do not allocate. Costs are integers (10 straight, 14
    /// diagonal) so heap ordering is exact.
    /// </summary>
    public class GridPathfinder
    {
        public const int StraightCost = 10;
        public const int DiagonalCost = 14;

        public OptimizedGrid Grid { get; }
        public bool AllowDiagonals { get; set; }

        /// <summary>Number of nodes expanded by the last search. Useful for visualisation.</summary>
        public int LastVisitedCount { get; private set; }

        private readonly int[] _neighbors = new int[OptimizedGrid.MaxNeighbors];
        private int[] _gCost;
        private int[] _fCost;
        private int[] _parent;
        private bool[] _closed;
        private int[] _heap;
        private int[] _heapSlot;
        private int _heapCount;

        public GridPathfinder(OptimizedGrid grid, bool allowDiagonals = true)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            AllowDiagonals = allowDiagonals;
        }

        /// <summary>True if the last search expanded this node.</summary>
        public bool WasVisited(int nodeIndex)
        {
            return _closed != null && nodeIndex >= 0 && nodeIndex < _closed.Length && _closed[nodeIndex];
        }

        /// <summary>
        /// Finds a path and writes it into <paramref name="path"/> as node indices from
        /// start to end inclusive. Returns false, leaving the list empty, when either
        /// end is out of bounds or occupied, or when no path exists.
        /// </summary>
        public bool FindPath(int startX, int startY, int endX, int endY, List<int> path)
        {
            path.Clear();

            if (!Grid.IsInBounds(startX, startY) || !Grid.IsInBounds(endX, endY))
                return false;

            int start = Grid.GetNodeIndex(startX, startY);
            int end = Grid.GetNodeIndex(endX, endY);

            if (Grid.Occupied[start] || Grid.Occupied[end])
                return false;

            Reset();

            _gCost[start] = 0;
            _fCost[start] = Heuristic(startX, startY, endX, endY);
            Push(start);

            while (_heapCount > 0)
            {
                int current = Pop();

                if (current == end)
                {
                    Reconstruct(end, path);
                    return true;
                }

                _closed[current] = true;
                LastVisitedCount++;

                Grid.GetNodeCoordinates(current, out int cx, out int cy);
                int neighborCount = Grid.GetNeighbors(cx, cy, _neighbors, AllowDiagonals);

                for (int i = 0; i < neighborCount; i++)
                {
                    int next = _neighbors[i];
                    if (_closed[next]) continue;

                    Grid.GetNodeCoordinates(next, out int nx, out int ny);
                    int step = nx != cx && ny != cy ? DiagonalCost : StraightCost;
                    int g = _gCost[current] + step;
                    if (g >= _gCost[next]) continue;

                    _gCost[next] = g;
                    _fCost[next] = g + Heuristic(nx, ny, endX, endY);
                    _parent[next] = current;

                    if (_heapSlot[next] < 0)
                        Push(next);
                    else
                        SiftUp(_heapSlot[next]);
                }
            }

            return false;
        }

        private int Heuristic(int ax, int ay, int bx, int by)
        {
            int dx = Math.Abs(ax - bx);
            int dy = Math.Abs(ay - by);

            if (!AllowDiagonals)
                return StraightCost * (dx + dy);

            // Octile distance: as many diagonal steps as possible, then straight ones.
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }

        private void Reset()
        {
            int count = Grid.NodeCount;

            if (_gCost == null || _gCost.Length != count)
            {
                _gCost = new int[count];
                _fCost = new int[count];
                _parent = new int[count];
                _closed = new bool[count];
                _heap = new int[count];
                _heapSlot = new int[count];
            }

            Array.Fill(_gCost, int.MaxValue);
            Array.Fill(_parent, -1);
            Array.Fill(_heapSlot, -1);
            Array.Clear(_closed, 0, count);
            _heapCount = 0;
            LastVisitedCount = 0;
        }

        private void Reconstruct(int end, List<int> path)
        {
            for (int node = end; node != -1; node = _parent[node])
            {
                path.Add(node);
            }

            path.Reverse();
        }

        // Binary min-heap keyed on _fCost. _heapSlot tracks each node's slot so a
        // node whose cost improves can be sifted up in place instead of re-pushed.

        private void Push(int node)
        {
            _heap[_heapCount] = node;
            _heapSlot[node] = _heapCount;
            _heapCount++;
            SiftUp(_heapCount - 1);
        }

        private int Pop()
        {
            int top = _heap[0];
            _heapCount--;
            _heapSlot[top] = -1;

            if (_heapCount > 0)
            {
                _heap[0] = _heap[_heapCount];
                _heapSlot[_heap[0]] = 0;
                SiftDown(0);
            }

            return top;
        }

        private void SiftUp(int slot)
        {
            int node = _heap[slot];

            while (slot > 0)
            {
                int parentSlot = (slot - 1) / 2;
                int parentNode = _heap[parentSlot];
                if (_fCost[parentNode] <= _fCost[node]) break;

                _heap[slot] = parentNode;
                _heapSlot[parentNode] = slot;
                slot = parentSlot;
            }

            _heap[slot] = node;
            _heapSlot[node] = slot;
        }

        private void SiftDown(int slot)
        {
            int node = _heap[slot];

            while (true)
            {
                int left = slot * 2 + 1;
                if (left >= _heapCount) break;

                int right = left + 1;
                int smallest = right < _heapCount && _fCost[_heap[right]] < _fCost[_heap[left]] ? right : left;
                if (_fCost[_heap[smallest]] >= _fCost[node]) break;

                _heap[slot] = _heap[smallest];
                _heapSlot[_heap[slot]] = slot;
                slot = smallest;
            }

            _heap[slot] = node;
            _heapSlot[node] = slot;
        }
    }
}
