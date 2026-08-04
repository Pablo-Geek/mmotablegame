using MMOTableGame.Hexes;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace MMOTableGame.Editor.Hexes
{
    [EditorTool("Hex Map Tool", typeof(HexMap))]
    public sealed class HexMapEditorTool : EditorTool
    {
        private enum EditMode
        {
            Place,
            Erase,
            Select
        }

        private static readonly int ControlHint = "HexMapEditorTool".GetHashCode();
        private EditMode mode;
        private GUIContent icon;

        public override GUIContent toolbarIcon => icon ??= new GUIContent(
            EditorGUIUtility.IconContent("Grid.BoxTool").image,
            "Hex Map Tool");

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView || target is not HexMap map)
            {
                return;
            }

            DrawGrid(map);
            DrawToolbar(map);

            if (mode == EditMode.Select)
            {
                return;
            }

            Event currentEvent = Event.current;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Vector3 layerOrigin = map.transform.TransformPoint(Vector3.up * map.ActiveLayerLocalHeight);
            Plane gridPlane = new(map.transform.up, layerOrigin);
            if (!gridPlane.Raycast(mouseRay, out float distance))
            {
                return;
            }

            Vector3 hitPoint = mouseRay.GetPoint(distance);
            HexCoordinates coordinates = map.WorldPositionToCoordinates(hitPoint);
            if (!map.Contains(coordinates))
            {
                return;
            }

            HexTileInstance occupiedTile = map.GetTile(coordinates, map.ActiveLayer);
            DrawCursor(map, coordinates, occupiedTile != null);
            DrawPlacementPreview((SceneView)window, map, coordinates, occupiedTile != null);

            int controlId = GUIUtility.GetControlID(ControlHint, FocusType.Passive);
            if (currentEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt)
            {
                return;
            }

            if (mode == EditMode.Place)
            {
                PlaceTile(map, coordinates, occupiedTile);
            }
            else if (mode == EditMode.Erase)
            {
                EraseTile(occupiedTile);
            }

            currentEvent.Use();
        }

        private void DrawGrid(HexMap map)
        {
            Matrix4x4 gridMatrix = map.transform.localToWorldMatrix *
                                   Matrix4x4.Translate(Vector3.up * map.ActiveLayerLocalHeight);

            using (new Handles.DrawingScope(map.GridColor, gridMatrix))
            {
                for (int q = -map.GridRadius; q <= map.GridRadius; q++)
                {
                    int minimumR = Mathf.Max(-map.GridRadius, -q - map.GridRadius);
                    int maximumR = Mathf.Min(map.GridRadius, -q + map.GridRadius);

                    for (int r = minimumR; r <= maximumR; r++)
                    {
                        HexCoordinates coordinates = new(q, r);
                        Vector3 center = HexGridMath.CoordinatesToLocalPosition(coordinates, map.HexRadius);
                        Vector3[] points = new Vector3[7];
                        for (int corner = 0; corner < 6; corner++)
                        {
                            points[corner] = HexGridMath.Corner(center, map.HexRadius, corner);
                        }

                        points[6] = points[0];
                        Handles.DrawAAPolyLine(1.5f, points);

                        if (map.ShowCoordinates)
                        {
                            Handles.Label(center, $"{q}, {r}", EditorStyles.miniLabel);
                        }
                    }
                }
            }
        }

        private void DrawToolbar(HexMap map)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12f, 12f, 260f, 70f), GUI.skin.window);
            mode = (EditMode)GUILayout.Toolbar((int)mode, new[] { "Place", "Erase", "Select" });

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("−", GUILayout.Width(36f)))
            {
                ChangeActiveLayer(map, map.ActiveLayer - 1);
            }

            GUILayout.Label($"Layer {map.ActiveLayer}  (Y {map.ActiveLayerLocalHeight:0.##})", EditorStyles.centeredGreyMiniLabel);

            if (GUILayout.Button("+", GUILayout.Width(36f)))
            {
                ChangeActiveLayer(map, map.ActiveLayer + 1);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void ChangeActiveLayer(HexMap map, int layer)
        {
            int clampedLayer = Mathf.Clamp(layer, 0, 50);
            if (clampedLayer == map.ActiveLayer)
            {
                return;
            }

            Undo.RecordObject(map, "Change Active Hex Layer");
            map.SetActiveLayer(clampedLayer);
            EditorUtility.SetDirty(map);
            SceneView.RepaintAll();
        }

        private static void DrawCursor(HexMap map, HexCoordinates coordinates, bool occupied)
        {
            Color color = occupied ? new Color(1f, 0.3f, 0.2f, 0.9f) : new Color(0.3f, 1f, 0.4f, 0.9f);
            Vector3 center = map.CoordinatesToWorldPosition(coordinates, map.ActiveLayer);
            Vector3[] points = new Vector3[7];

            for (int corner = 0; corner < 6; corner++)
            {
                Vector3 localCorner = HexGridMath.Corner(Vector3.zero, map.HexRadius * 0.92f, corner);
                points[corner] = center + map.transform.TransformVector(localCorner);
            }

            points[6] = points[0];
            Handles.color = color;
            Handles.DrawAAPolyLine(4f, points);
            Handles.Label(center + map.transform.up * 0.05f, $"  {coordinates}  L{map.ActiveLayer}");
        }

        private void DrawPlacementPreview(
            SceneView sceneView,
            HexMap map,
            HexCoordinates coordinates,
            bool occupied)
        {
            if (mode != EditMode.Place || occupied || map.PlacementPrefab == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            GameObject prefab = map.PlacementPrefab;
            Vector3 localPosition = map.CoordinatesToLocalPosition(coordinates, map.ActiveLayer);
            Matrix4x4 previewRootMatrix = map.transform.localToWorldMatrix * Matrix4x4.TRS(
                localPosition,
                prefab.transform.localRotation,
                prefab.transform.localScale);

            MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshFilter.sharedMesh == null || meshRenderer == null || !meshRenderer.enabled)
                {
                    continue;
                }

                Matrix4x4 relativeMatrix = prefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                Matrix4x4 drawMatrix = previewRootMatrix * relativeMatrix;
                Material[] materials = meshRenderer.sharedMaterials;
                int subMeshCount = meshFilter.sharedMesh.subMeshCount;

                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    if (materials.Length == 0)
                    {
                        continue;
                    }

                    Material material = materials[Mathf.Min(subMesh, materials.Length - 1)];
                    if (material != null)
                    {
                        Graphics.DrawMesh(
                            meshFilter.sharedMesh,
                            drawMatrix,
                            material,
                            prefab.layer,
                            sceneView.camera,
                            subMesh);
                    }
                }
            }
        }

        private static void PlaceTile(HexMap map, HexCoordinates coordinates, HexTileInstance occupiedTile)
        {
            if (occupiedTile != null)
            {
                return;
            }

            GameObject prefab = map.PlacementPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("Assign a Placement Prefab before placing hex tiles.", map);
                return;
            }

            GameObject instance;
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, map.transform);
            }
            else
            {
                instance = Object.Instantiate(prefab, map.transform);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place Hex Tile");
            instance.transform.localPosition = map.CoordinatesToLocalPosition(coordinates, map.ActiveLayer);
            instance.name = $"Hex_{coordinates.Q}_{coordinates.R}_L{map.ActiveLayer}_{prefab.name}";

            HexTileInstance tile = instance.GetComponent<HexTileInstance>();
            if (tile == null)
            {
                tile = Undo.AddComponent<HexTileInstance>(instance);
            }

            tile.SetPlacement(coordinates, map.ActiveLayer);
            EditorUtility.SetDirty(tile);
        }

        private static void EraseTile(HexTileInstance tile)
        {
            if (tile != null)
            {
                Undo.DestroyObjectImmediate(tile.gameObject);
            }
        }
    }
}
