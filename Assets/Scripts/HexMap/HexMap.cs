using UnityEngine;

namespace MMOTableGame.Hexes
{
    [DisallowMultipleComponent]
    public sealed class HexMap : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(0.01f)] private float hexRadius = 1f;
        [SerializeField, Range(1, 50)] private int gridRadius = 10;
        [SerializeField, Min(0.01f)] private float layerHeight = 0.5f;
        [SerializeField, Range(0, 50)] private int activeLayer;

        [Header("Placement")]
        [SerializeField] private GameObject placementPrefab;

        [Header("Scene View")]
        [SerializeField] private Color gridColor = new(0.2f, 0.8f, 1f, 0.65f);
        [SerializeField] private bool showCoordinates;

        public float HexRadius => Mathf.Max(0.01f, hexRadius);
        public int GridRadius => Mathf.Max(1, gridRadius);
        public float LayerHeight => Mathf.Max(0.01f, layerHeight);
        public int ActiveLayer => Mathf.Max(0, activeLayer);
        public float ActiveLayerLocalHeight => ActiveLayer * LayerHeight;
        public GameObject PlacementPrefab => placementPrefab;
        public Color GridColor => gridColor;
        public bool ShowCoordinates => showCoordinates;

        public Vector3 CoordinatesToWorldPosition(HexCoordinates coordinates)
        {
            return CoordinatesToWorldPosition(coordinates, 0);
        }

        public Vector3 CoordinatesToWorldPosition(HexCoordinates coordinates, int layer)
        {
            return transform.TransformPoint(CoordinatesToLocalPosition(coordinates, layer));
        }

        public Vector3 CoordinatesToLocalPosition(HexCoordinates coordinates, int layer)
        {
            Vector3 position = HexGridMath.CoordinatesToLocalPosition(coordinates, HexRadius);
            position.y = Mathf.Max(0, layer) * LayerHeight;
            return position;
        }

        public HexCoordinates WorldPositionToCoordinates(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            return HexGridMath.LocalPositionToCoordinates(localPosition, HexRadius);
        }

        public bool Contains(HexCoordinates coordinates)
        {
            return HexGridMath.IsInsideRadius(coordinates, GridRadius);
        }

        public HexTileInstance GetTile(HexCoordinates coordinates)
        {
            return GetTile(coordinates, 0);
        }

        public HexTileInstance GetTile(HexCoordinates coordinates, int layer)
        {
            HexTileInstance[] tiles = GetComponentsInChildren<HexTileInstance>(true);
            foreach (HexTileInstance tile in tiles)
            {
                if (tile.transform.parent == transform &&
                    tile.Coordinates == coordinates &&
                    tile.Layer == layer)
                {
                    return tile;
                }
            }

            return null;
        }

        public void SetActiveLayer(int layer)
        {
            activeLayer = Mathf.Clamp(layer, 0, 50);
        }

        private void OnValidate()
        {
            hexRadius = Mathf.Max(0.01f, hexRadius);
            gridRadius = Mathf.Clamp(gridRadius, 1, 50);
            layerHeight = Mathf.Max(0.01f, layerHeight);
            activeLayer = Mathf.Clamp(activeLayer, 0, 50);
        }
    }
}
