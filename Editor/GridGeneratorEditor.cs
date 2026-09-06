using UnityEditor;
using UnityEngine;

namespace TOMICZ.Grid.Editor
{
    [CustomEditor(typeof(GridGenerator))]
    public class GridGeneratorEditor : UnityEditor.Editor
    {
        private const int LargeGridNodes = 250_000;
        private const int PositionBytes = 12;
        private const int ColorBytes = 4;

        private GridLayoutAsset _layout;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var generator = (GridGenerator)target;
            OptimizedGrid grid = generator.Grid;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);

            if (grid != null)
            {
                int vertices = grid.Vertices.Length;
                int indexBytes = vertices > ushort.MaxValue ? 4 : 2;
                long bytes = (long)vertices * (PositionBytes + ColorBytes) + (long)grid.Triangles.Length * indexBytes;

                EditorGUILayout.LabelField("Nodes", grid.NodeCount.ToString("N0"));
                EditorGUILayout.LabelField("Vertices", vertices.ToString("N0"));
                EditorGUILayout.LabelField("Mesh data", EditorUtility.FormatBytes(bytes));

                if (grid.NodeCount > LargeGridNodes)
                {
                    EditorGUILayout.HelpBox(
                        $"Grids above {LargeGridNodes:N0} nodes rebuild noticeably slowly on every inspector change.",
                        MessageType.Warning);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Regenerate"))
                {
                    generator.RegenerateGrid();
                }

                if (GUILayout.Button("Clear Occupancy"))
                {
                    Undo.RecordObject(generator, "Clear Grid Occupancy");
                    generator.ClearOccupancy();
                    EditorUtility.SetDirty(generator);
                }

                if (GUILayout.Button("Reset Colors"))
                {
                    Undo.RecordObject(generator, "Reset Grid Colors");
                    generator.SetAllNodeColors(OptimizedGrid.White);
                    EditorUtility.SetDirty(generator);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            _layout = (GridLayoutAsset)EditorGUILayout.ObjectField("Layout Asset", _layout, typeof(GridLayoutAsset), false);

            using (new EditorGUI.DisabledScope(_layout == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save To Asset"))
                {
                    Undo.RecordObject(_layout, "Save Grid Layout");
                    generator.SaveLayout(_layout);
                }

                if (GUILayout.Button("Load From Asset"))
                {
                    Undo.RecordObject(generator, "Load Grid Layout");
                    if (!generator.LoadLayout(_layout))
                    {
                        Debug.LogWarning($"'{_layout.name}' is not a valid grid layout.", _layout);
                    }
                    EditorUtility.SetDirty(generator);
                }
            }
        }
    }
}
