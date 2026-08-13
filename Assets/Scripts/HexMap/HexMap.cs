using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MMOTableGame.Hexes
{
    [DisallowMultipleComponent]
    public sealed class HexMap : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(0.01f)] private float hexRadius = 1f;
        [SerializeField, Range(1, 50)] private int gridRadius = 10;
        [FormerlySerializedAs("layerHeight")]
        [SerializeField, Min(0.01f)] private float defaultLayerSpacing = 0.5f;
        [SerializeField, Range(0, 50)] private int activeLayer;
        [SerializeField, HideInInspector] private List<float> layerHeights = new();

        [Header("Placement")]
        [SerializeField] private GameObject placementPrefab;

        [Header("Scene View")]
        [SerializeField] private Color gridColor = new(0.2f, 0.8f, 1f, 0.65f);
        [SerializeField] private bool showCoordinates;

        public float HexRadius => Mathf.Max(0.01f, hexRadius);
        public int GridRadius => Mathf.Max(1, gridRadius);
        public float DefaultLayerSpacing => Mathf.Max(0.01f, defaultLayerSpacing);
        public int ActiveLayer => Mathf.Max(0, activeLayer);
        public float ActiveLayerLocalHeight => GetLayerHeight(ActiveLayer);
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
            position.y = GetLayerHeight(layer);
            return position;
        }

        public float GetLayerHeight(int layer)
        {
            int safeLayer = Mathf.Max(0, layer);
            if (layerHeights != null && safeLayer < layerHeights.Count)
            {
                return Mathf.Max(0f, layerHeights[safeLayer]);
            }

            return safeLayer * DefaultLayerSpacing;
        }

        public void SetLayerHeight(int layer, float height)
        {
            int safeLayer = Mathf.Clamp(layer, 0, 50);
            EnsureLayerHeightExists(safeLayer);
            layerHeights[safeLayer] = Mathf.Max(0f, height);
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
            EnsureLayerHeightExists(activeLayer);
        }

        private void OnValidate()
        {
            hexRadius = Mathf.Max(0.01f, hexRadius);
            gridRadius = Mathf.Clamp(gridRadius, 1, 50);
            defaultLayerSpacing = Mathf.Max(0.01f, defaultLayerSpacing);
            activeLayer = Mathf.Clamp(activeLayer, 0, 50);
            EnsureLayerHeightExists(activeLayer);
        }

        private void EnsureLayerHeightExists(int layer)
        {
            layerHeights ??= new List<float>();
            while (layerHeights.Count <= layer)
            {
                float nextHeight = layerHeights.Count == 0
                    ? 0f
                    : layerHeights[^1] + DefaultLayerSpacing;
                layerHeights.Add(nextHeight);
            }
        }
    }
}
