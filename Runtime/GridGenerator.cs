using UnityEngine;
using UnityEngine.Serialization;

namespace TOMICZ.Grid
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GridGenerator : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("gridWidth")] private int _gridWidth = 10;
        [SerializeField, FormerlySerializedAs("gridHeight")] private int _gridHeight = 10;
        [SerializeField, FormerlySerializedAs("spacing")] private float _spacing = .1f;
        [SerializeField, FormerlySerializedAs("nodeWidth")] private float _nodeWidth = 1f;
        [SerializeField, FormerlySerializedAs("nodeHeight")] private float _nodeHeight = 1f;
        [SerializeField, FormerlySerializedAs("isHorizontal")] private bool _isHorizontal;
        [SerializeField] private Material _defaultMaterial;

        // Node state survives regeneration and scene saves. The grid works on
        // these arrays directly, so there is a single source of truth.
        [SerializeField, HideInInspector] private bool[] _occupied;
        [SerializeField, HideInInspector] private Color32[] _nodeColors;
        [SerializeField, HideInInspector] private int _stateWidth;
        [SerializeField, HideInInspector] private int _stateHeight;

        private Mesh _mesh;
        private OptimizedGrid _grid;

        public OptimizedGrid Grid => _grid;
        public bool IsHorizontal => _isHorizontal;

        public void SetNodeColor(int x, int y, Color32 color)
        {
            if (_grid == null) return;

            _grid.SetNodeColor(x, y, color);
            _grid.LoadMeshColors(_mesh);
        }

        public Color32 GetNodeColor(int x, int y)
        {
            return _grid != null ? _grid.GetNodeColor(x, y) : default;
        }

        public void SetNodeOccupied(int x, int y, bool occupied)
        {
            _grid?.SetNodeOccupied(x, y, occupied);
        }

        public bool IsNodeOccupied(int x, int y)
        {
            return _grid != null && _grid.IsNodeOccupied(x, y);
        }

        /// <summary>World-space center of a node.</summary>
        public Vector3 NodeToWorld(int x, int y)
        {
            return _grid != null ? transform.TransformPoint(_grid.GetNodeCenter(x, y)) : transform.position;
        }

        /// <summary>Maps a world position to node coordinates. Returns false outside the grid.</summary>
        public bool WorldToNode(Vector3 worldPosition, out int x, out int y)
        {
            if (_grid == null)
            {
                x = y = -1;
                return false;
            }

            return _grid.TryGetNode(transform.InverseTransformPoint(worldPosition), out x, out y);
        }

        /// <summary>
        /// Rebuilds the grid and its mesh from the current settings. Node occupancy
        /// and colors are kept; when the dimensions change, the overlapping region
        /// is preserved and new nodes start empty and white.
        /// Runs automatically on enable and whenever a value changes in the inspector.
        /// </summary>
        public void RegenerateGrid()
        {
            EnsureMesh();
            SyncNodeState();

            _grid = new OptimizedGrid(_gridWidth, _gridHeight, _nodeWidth, _nodeHeight, _spacing, _occupied, _nodeColors);
            _grid.GenerateGrid(_isHorizontal);
            _grid.LoadMeshData(_mesh);
        }

        private void OnEnable()
        {
            // OnValidate only runs in the editor, so this is what builds the grid
            // in a player. It also restores the mesh after a domain reload.
            ApplyMaterial();
            RegenerateGrid();
        }

        private void OnDestroy()
        {
            if (_mesh != null && (_mesh.hideFlags & HideFlags.DontSave) != 0)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }
        }

        private void OnValidate()
        {
            _gridWidth = Mathf.Max(0, _gridWidth);
            _gridHeight = Mathf.Max(0, _gridHeight);

#if UNITY_EDITOR
            // Unity disallows creating or destroying objects inside OnValidate,
            // so the rebuild is deferred to the next editor update.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || !isActiveAndEnabled) return;
                RegenerateGrid();
            };
#endif
        }

        private void ApplyMaterial()
        {
            if (_defaultMaterial == null) return;

            // sharedMaterial avoids instantiating a copy per component, which
            // leaks a material into the scene every time Awake runs in edit mode.
            GetComponent<MeshRenderer>().sharedMaterial = _defaultMaterial;
        }

        private void EnsureMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();

            if (_mesh == null)
            {
                // Reuse a mesh this component created earlier (it survives domain
                // reloads), but never adopt a saved mesh: the grid is always rebuilt
                // from its settings, so saving it into the scene is pure bloat.
                Mesh existing = meshFilter.sharedMesh;
                if (existing != null && (existing.hideFlags & HideFlags.DontSave) != 0)
                {
                    _mesh = existing;
                }
                else
                {
                    _mesh = new Mesh { name = "Grid Mesh", hideFlags = HideFlags.DontSave };
                }
            }

            if (meshFilter.sharedMesh != _mesh)
            {
                meshFilter.sharedMesh = _mesh;
            }
        }

        private void SyncNodeState()
        {
            _occupied = ResizeNodeState(_occupied, _stateWidth, _stateHeight, _gridWidth, _gridHeight, false);
            _nodeColors = ResizeNodeState(_nodeColors, _stateWidth, _stateHeight, _gridWidth, _gridHeight, OptimizedGrid.White);
            _stateWidth = _gridWidth;
            _stateHeight = _gridHeight;
        }

        private static T[] ResizeNodeState<T>(T[] source, int oldWidth, int oldHeight, int newWidth, int newHeight, T fill)
        {
            int newCount = Mathf.Max(0, newWidth) * Mathf.Max(0, newHeight);
            bool sourceValid = source != null && source.Length == Mathf.Max(0, oldWidth) * Mathf.Max(0, oldHeight);

            if (sourceValid && oldWidth == newWidth && oldHeight == newHeight)
                return source;

            var result = new T[newCount];
            for (int i = 0; i < newCount; i++)
            {
                result[i] = fill;
            }

            if (sourceValid)
            {
                int copyWidth = Mathf.Min(oldWidth, newWidth);
                int copyHeight = Mathf.Min(oldHeight, newHeight);
                for (int y = 0; y < copyHeight; y++)
                {
                    System.Array.Copy(source, y * oldWidth, result, y * newWidth, copyWidth);
                }
            }

            return result;
        }
    }
}
