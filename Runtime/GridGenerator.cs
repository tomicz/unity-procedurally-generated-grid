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

        private Mesh _mesh;
        private OptimizedGrid _grid;

        public OptimizedGrid Grid => _grid;
        public bool IsHorizontal => _isHorizontal;

        public void SetNodeColor(int x, int y, Color color)
        {
            if (_grid == null) return;

            _grid.SetNodeColor(x, y, color);
            _grid.LoadMeshData(_mesh);
        }

        /// <summary>
        /// Rebuilds the grid and its mesh from the current settings.
        /// Runs automatically on enable and whenever a value changes in the inspector.
        /// </summary>
        public void RegenerateGrid()
        {
            EnsureMesh();

            _grid = new OptimizedGrid(_gridWidth, _gridHeight, _nodeWidth, _nodeHeight, _spacing);
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
    }
}
