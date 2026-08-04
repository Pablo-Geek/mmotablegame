using MMOTableGame.Hexes;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace MMOTableGame.Editor.Hexes
{
    [CustomEditor(typeof(HexMap))]
    public sealed class HexMapInspector : UnityEditor.Editor
    {
        [MenuItem("GameObject/Hex Map", false, 10)]
        private static void CreateHexMap(MenuCommand menuCommand)
        {
            GameObject mapObject = new("Hex Map");
            GameObjectUtility.SetParentAndAlign(mapObject, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(mapObject, "Create Hex Map");
            mapObject.AddComponent<HexMap>();
            Selection.activeGameObject = mapObject;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Activate the tool, choose the active layer with −/+ in the Scene View, " +
                "then use Place, Erase or Select. Layer Height controls the vertical spacing. " +
                "The grid is an editor guide and is not rendered in the game.",
                MessageType.Info);

            if (GUILayout.Button("Activate Hex Map Tool", GUILayout.Height(28f)))
            {
                ToolManager.SetActiveTool<HexMapEditorTool>();
                SceneView.lastActiveSceneView?.Focus();
            }
        }
    }
}
