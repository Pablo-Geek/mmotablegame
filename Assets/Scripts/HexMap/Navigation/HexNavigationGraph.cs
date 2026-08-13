using System.Collections.Generic;
using UnityEngine;

namespace MMOTableGame.Hexes.Navigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HexMap))]
    public sealed class HexNavigationGraph : MonoBehaviour
    {
        private static readonly HexCoordinates[] NeighborDirections =
        {
            new(1, 0),
            new(1, -1),
            new(0, -1),
            new(-1, 0),
            new(-1, 1),
            new(0, 1)
        };

        [SerializeField] private HexMap hexMap;
        [Tooltip("Máxima diferencia de layers permitida mediante una rampa. Los tiles normales nunca cambian de layer.")]
        [SerializeField, Range(0, 10)] private int maxStepLayers = 1;

        private readonly Dictionary<HexCoordinates, HexNavigationNode> surfaceNodes = new();
        private readonly List<HexNavigationNode> nodes = new();

        public HexMap Map => hexMap;
        public int MaxStepLayers => maxStepLayers;
        public IReadOnlyList<HexNavigationNode> Nodes => nodes;
        public bool IsReady => hexMap != null && nodes.Count > 0;

        private void Awake()
        {
            if (hexMap == null)
            {
                hexMap = GetComponent<HexMap>();
            }

            Rebuild();
        }

        [ContextMenu("Rebuild Navigation Graph")]
        public void Rebuild()
        {
            surfaceNodes.Clear();
            nodes.Clear();

            if (hexMap == null)
            {
                hexMap = GetComponent<HexMap>();
            }

            if (hexMap == null)
            {
                return;
            }

            HexTileInstance[] tiles = hexMap.GetComponentsInChildren<HexTileInstance>(true);
            foreach (HexTileInstance tile in tiles)
            {
                if (!tile.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (surfaceNodes.TryGetValue(tile.Coordinates, out HexNavigationNode currentTop) &&
                    currentTop.Layer >= tile.Layer)
                {
                    continue;
                }

                surfaceNodes[tile.Coordinates] = new HexNavigationNode(tile, CalculateNavigationPosition(tile));
            }

            nodes.AddRange(surfaceNodes.Values);
        }

        public void Configure(HexMap map, int maximumStepLayers)
        {
            hexMap = map;
            maxStepLayers = Mathf.Max(0, maximumStepLayers);
            Rebuild();
        }

        public bool TryGetNode(HexCoordinates coordinates, out HexNavigationNode node)
        {
            return surfaceNodes.TryGetValue(coordinates, out node);
        }

        public bool TryGetClosestNode(Vector3 worldPosition, out HexNavigationNode closestNode)
        {
            closestNode = null;
            if (hexMap == null || nodes.Count == 0)
            {
                return false;
            }

            HexCoordinates coordinates = hexMap.WorldPositionToCoordinates(worldPosition);
            if (surfaceNodes.TryGetValue(coordinates, out closestNode))
            {
                return true;
            }

            float closestDistance = float.PositiveInfinity;
            Vector3 mapUp = hexMap.transform.up;
            foreach (HexNavigationNode node in nodes)
            {
                Vector3 planarDifference = Vector3.ProjectOnPlane(node.WorldPosition - worldPosition, mapUp);
                float distance = planarDifference.sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }

            return closestNode != null;
        }

        public void GetNeighbors(HexNavigationNode node, List<HexNavigationNode> results)
        {
            results.Clear();

            HexRamp nodeRamp = node.Tile.GetComponent<HexRamp>();
            if (nodeRamp != null)
            {
                AddRampEndpoints(node, nodeRamp, results);
                return;
            }

            foreach (HexCoordinates direction in NeighborDirections)
            {
                HexCoordinates coordinates = new(
                    node.Coordinates.Q + direction.Q,
                    node.Coordinates.R + direction.R);

                if (!surfaceNodes.TryGetValue(coordinates, out HexNavigationNode neighbor))
                {
                    continue;
                }

                HexRamp neighborRamp = neighbor.Tile.GetComponent<HexRamp>();
                if (neighborRamp == null)
                {
                    if (neighbor.Layer == node.Layer)
                    {
                        results.Add(neighbor);
                    }

                    continue;
                }

                if (RampConnectsToNode(neighbor, neighborRamp, node))
                {
                    results.Add(neighbor);
                }
            }
        }

        private void AddRampEndpoints(
            HexNavigationNode rampNode,
            HexRamp ramp,
            List<HexNavigationNode> results)
        {
            if (surfaceNodes.TryGetValue(
                    ramp.GetDownhillCoordinates(rampNode.Coordinates),
                    out HexNavigationNode downhill) &&
                downhill.Layer == rampNode.Layer &&
                EndpointAcceptsRamp(downhill, rampNode))
            {
                results.Add(downhill);
            }

            if (ramp.LayerDelta > maxStepLayers)
            {
                return;
            }

            if (surfaceNodes.TryGetValue(
                    ramp.GetUphillCoordinates(rampNode.Coordinates),
                    out HexNavigationNode uphill) &&
                uphill.Layer == rampNode.Layer + ramp.LayerDelta &&
                EndpointAcceptsRamp(uphill, rampNode))
            {
                results.Add(uphill);
            }
        }

        private bool EndpointAcceptsRamp(HexNavigationNode endpoint, HexNavigationNode rampNode)
        {
            HexRamp endpointRamp = endpoint.Tile.GetComponent<HexRamp>();
            return endpointRamp == null || RampConnectsToNode(endpoint, endpointRamp, rampNode);
        }

        private bool RampConnectsToNode(
            HexNavigationNode rampNode,
            HexRamp ramp,
            HexNavigationNode candidate)
        {
            if (candidate.Coordinates == ramp.GetDownhillCoordinates(rampNode.Coordinates))
            {
                return candidate.Layer == rampNode.Layer;
            }

            return ramp.LayerDelta <= maxStepLayers &&
                   candidate.Coordinates == ramp.GetUphillCoordinates(rampNode.Coordinates) &&
                   candidate.Layer == rampNode.Layer + ramp.LayerDelta;
        }

        public bool TryPickNode(Ray ray, out HexNavigationNode pickedNode)
        {
            pickedNode = null;
            if (hexMap == null)
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;
            Vector3 mapUp = hexMap.transform.up;

            foreach (HexNavigationNode node in nodes)
            {
                Plane surfacePlane = new(mapUp, node.WorldPosition);
                if (!surfacePlane.Raycast(ray, out float distance) || distance < 0f || distance >= closestDistance)
                {
                    continue;
                }

                Vector3 hitPoint = ray.GetPoint(distance);
                if (hexMap.WorldPositionToCoordinates(hitPoint) != node.Coordinates)
                {
                    continue;
                }

                closestDistance = distance;
                pickedNode = node;
            }

            return pickedNode != null;
        }

        private Vector3 CalculateNavigationPosition(HexTileInstance tile)
        {
            HexRamp ramp = tile.GetComponent<HexRamp>();
            if (ramp != null)
            {
                return ramp.GetNavigationPosition();
            }

            Vector3 surfacePosition = hexMap.CoordinatesToWorldPosition(tile.Coordinates, tile.Layer);
            Vector3 mapUp = hexMap.transform.up;
            float topProjection = Vector3.Dot(surfacePosition, mapUp);

            Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer tileRenderer in renderers)
            {
                Bounds bounds = tileRenderer.bounds;
                Vector3 extents = bounds.extents;
                float projectedExtent =
                    Mathf.Abs(mapUp.x) * extents.x +
                    Mathf.Abs(mapUp.y) * extents.y +
                    Mathf.Abs(mapUp.z) * extents.z;
                topProjection = Mathf.Max(topProjection, Vector3.Dot(bounds.center, mapUp) + projectedExtent);
            }

            float currentProjection = Vector3.Dot(surfacePosition, mapUp);
            return surfacePosition + mapUp * (topProjection - currentProjection);
        }

        private void OnValidate()
        {
            maxStepLayers = Mathf.Max(0, maxStepLayers);
        }
    }
}
