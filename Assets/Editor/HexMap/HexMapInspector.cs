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

            HexMap map = (HexMap)target;
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            float activeLayerHeight = EditorGUILayout.FloatField(
                $"Layer {map.ActiveLayer} Height",
                map.ActiveLayerLocalHeight);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(map, "Change Hex Layer Height");
                map.SetLayerHeight(map.ActiveLayer, activeLayerHeight);
                ResnapLayer(map, map.ActiveLayer);
                EditorUtility.SetDirty(map);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Resnap Active Layer"))
            {
                ResnapLayer(map, map.ActiveLayer);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Activate the tool, choose the active layer with −/+ in the Scene View, " +
                "then use Place, Erase or Select. Each layer remembers its own absolute height. " +
                "Default Layer Spacing initializes new layers. " +
                "The grid is an editor guide and is not rendered in the game.",
                MessageType.Info);

            if (GUILayout.Button("Activate Hex Map Tool", GUILayout.Height(28f)))
            {
                ToolManager.SetActiveTool<HexMapEditorTool>();
                SceneView.lastActiveSceneView?.Focus();
            }
        }

        private static void ResnapLayer(HexMap map, int layer)
        {
            HexTileInstance[] tiles = map.GetComponentsInChildren<HexTileInstance>(true);
            foreach (HexTileInstance tile in tiles)
            {
                if (tile.Layer != layer)
                {
                    continue;
                }

                Undo.RecordObject(tile.transform, "Resnap Hex Layer");
                tile.transform.localPosition = map.CoordinatesToLocalPosition(tile.Coordinates, layer);
                EditorUtility.SetDirty(tile.transform);
            }
        }
    }
}
