using UnityEngine;

namespace TOMICZ.Grid
{
    /// <summary>
    /// A saved grid layout: dimensions plus per-node occupancy and color. Create
    /// one from the Assets menu, then use GridGenerator.SaveLayout and LoadLayout
    /// to move a layout between scenes or swap layouts at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "Grid Layout", menuName = "Procedural Grid/Layout")]
    public class GridLayoutAsset : ScriptableObject
    {
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField, HideInInspector] private bool[] _occupied = System.Array.Empty<bool>();
        [SerializeField, HideInInspector] private byte[] _nodeColorBytes = System.Array.Empty<byte>();

        public int Width => _width;
        public int Height => _height;
        public int NodeCount => Mathf.Max(0, _width) * Mathf.Max(0, _height);

        /// <summary>True when the stored arrays match the declared dimensions.</summary>
        public bool IsValid =>
            _occupied != null && _nodeColorBytes != null &&
            _occupied.Length == NodeCount && _nodeColorBytes.Length == NodeCount * Color32Packing.BytesPerColor;

        /// <summary>Snapshots a grid's dimensions, occupancy and colors into this asset.</summary>
        public void CopyFrom(OptimizedGrid grid)
        {
            _width = grid.GridWidth;
            _height = grid.GridHeight;
            _occupied = (bool[])grid.Occupied.Clone();
            _nodeColorBytes = Color32Packing.Pack(grid.NodeColors);
        }

        /// <summary>
        /// Writes this asset's state into a grid of the same dimensions. Returns
        /// false without touching the grid when the sizes differ or the asset is invalid.
        /// </summary>
        public bool CopyTo(OptimizedGrid grid)
        {
            if (!IsValid || grid.GridWidth != _width || grid.GridHeight != _height)
                return false;

            System.Array.Copy(_occupied, grid.Occupied, NodeCount);

            Color32[] colors = Color32Packing.Unpack(_nodeColorBytes);
            for (int i = 0; i < NodeCount; i++)
            {
                grid.SetNodeColor(i, colors[i]);
            }

            return true;
        }
    }
}
