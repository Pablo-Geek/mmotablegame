using System.Collections.Generic;
using MMOTableGame.CameraSystem;
using MMOTableGame.Hexes;
using MMOTableGame.Hexes.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MMOTableGame.PlayerSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class HexPlayerAgent : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private HexNavigationGraph navigationGraph;
        [SerializeField] private Camera inputCamera;
        [SerializeField, Min(0.01f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float surfaceOffset;

        [Header("Debug")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private Color pathColor = new(0.2f, 1f, 0.35f, 1f);
        [SerializeField] private Color destinationColor = new(1f, 0.8f, 0.15f, 1f);

        private readonly List<HexNavigationNode> currentPath = new();
        private LineRenderer pathLine;
        private LineRenderer destinationLine;
        private Material debugMaterial;
        private int waypointIndex;
        private HexNavigationNode currentNode;
        private HexNavigationNode destinationNode;

        public bool IsMoving => waypointIndex < currentPath.Count;
        public HexNavigationNode CurrentNode => currentNode;
        public HexNavigationNode DestinationNode => destinationNode;
        public IReadOnlyList<HexNavigationNode> CurrentPath => currentPath;

        private void Awake()
        {
            if (navigationGraph == null)
            {
                navigationGraph = FindFirstObjectByType<HexNavigationGraph>();
            }

            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }

            EnsureCameraFollowsPlayer();
            ConfigureDebugLines();
        }

        private void Start()
        {
            if (navigationGraph == null)
            {
                Debug.LogError("HexPlayerAgent requires a HexNavigationGraph.", this);
                enabled = false;
                return;
            }

            navigationGraph.Rebuild();
            if (!navigationGraph.TryGetClosestNode(transform.position, out currentNode))
            {
                Debug.LogError("Player could not be matched to a navigation tile.", this);
                enabled = false;
                return;
            }

            transform.position = GetAgentPosition(currentNode);
        }

        private void Update()
        {
            HandleDestinationInput();
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            MoveAlongPath(Mathf.Max(0f, deltaTime));
        }

        public bool MoveTo(HexNavigationNode destination)
        {
            if (destination == null || navigationGraph == null)
            {
                return false;
            }

            if (!navigationGraph.TryGetClosestNode(transform.position, out HexNavigationNode start))
            {
                return false;
            }

            currentPath.Clear();
            waypointIndex = 0;
            if (!HexPathfinder.TryFindPath(navigationGraph, start, destination, currentPath))
            {
                destinationNode = null;
                ClearDebugLines();
                Debug.LogWarning($"No path exists from {start.Key} to {destination.Key}.", this);
                return false;
            }

            currentNode = start;
            destinationNode = destination;
            waypointIndex = currentPath.Count > 1 ? 1 : currentPath.Count;
            UpdateDebugLines();
            return true;
        }

        public bool MoveTo(HexCoordinates coordinates)
        {
            return navigationGraph != null &&
                   navigationGraph.TryGetNode(coordinates, out HexNavigationNode destination) &&
                   MoveTo(destination);
        }

        private void HandleDestinationInput()
        {
            if (Mouse.current == null || inputCamera == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Ray ray = inputCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (navigationGraph.TryPickNode(ray, out HexNavigationNode pickedNode))
            {
                MoveTo(pickedNode);
            }
        }

        private void MoveAlongPath(float deltaTime)
        {
            if (!IsMoving)
            {
                return;
            }

            float remainingDistance = moveSpeed * deltaTime;
            while (remainingDistance > 0f && waypointIndex < currentPath.Count)
            {
                HexNavigationNode waypoint = currentPath[waypointIndex];
                Vector3 targetPosition = GetAgentPosition(waypoint);
                float distance = Vector3.Distance(transform.position, targetPosition);

                if (distance <= remainingDistance)
                {
                    transform.position = targetPosition;
                    remainingDistance -= distance;
                    currentNode = waypoint;
                    waypointIndex++;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, remainingDistance);
                    remainingDistance = 0f;
                }
            }

            if (!IsMoving)
            {
                destinationNode = null;
                ClearDebugLines();
            }
        }

        private Vector3 GetAgentPosition(HexNavigationNode node)
        {
            return node.WorldPosition + navigationGraph.Map.transform.up * surfaceOffset;
        }

        private void EnsureCameraFollowsPlayer()
        {
            if (inputCamera == null)
            {
                return;
            }

            IsometricCameraFollow cameraFollow = inputCamera.GetComponent<IsometricCameraFollow>();
            if (cameraFollow != null && cameraFollow.Target != transform)
            {
                cameraFollow.SetTarget(transform);
            }
        }

        private void ConfigureDebugLines()
        {
            pathLine = GetComponent<LineRenderer>();
            ConfigureLine(pathLine, pathColor, 0.07f);

            GameObject destinationObject = new("Path Destination");
            destinationObject.transform.SetParent(transform, false);
            destinationObject.hideFlags = HideFlags.DontSave;
            destinationLine = destinationObject.AddComponent<LineRenderer>();
            ConfigureLine(destinationLine, destinationColor, 0.1f);
            ClearDebugLines();
        }

        private void ConfigureLine(LineRenderer line, Color color, float width)
        {
            line.useWorldSpace = true;
            line.loop = false;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            if (debugMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                debugMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            }

            line.sharedMaterial = debugMaterial;
        }

        private void UpdateDebugLines()
        {
            if (!showPath || currentPath.Count == 0)
            {
                ClearDebugLines();
                return;
            }

            Vector3 mapUp = navigationGraph.Map.transform.up;
            pathLine.enabled = true;
            pathLine.positionCount = currentPath.Count;
            for (int index = 0; index < currentPath.Count; index++)
            {
                pathLine.SetPosition(index, GetAgentPosition(currentPath[index]) + mapUp * 0.08f);
            }

            destinationLine.enabled = destinationNode != null;
            if (destinationNode == null)
            {
                return;
            }

            destinationLine.positionCount = 7;
            Vector3 center = GetAgentPosition(destinationNode) + mapUp * 0.1f;
            for (int corner = 0; corner < 6; corner++)
            {
                Vector3 localCorner = HexGridMath.Corner(
                    Vector3.zero,
                    navigationGraph.Map.HexRadius * 0.82f,
                    corner);
                destinationLine.SetPosition(
                    corner,
                    center + navigationGraph.Map.transform.TransformVector(localCorner));
            }

            destinationLine.SetPosition(6, destinationLine.GetPosition(0));
        }

        private void ClearDebugLines()
        {
            if (pathLine != null)
            {
                pathLine.positionCount = 0;
                pathLine.enabled = false;
            }

            if (destinationLine != null)
            {
                destinationLine.positionCount = 0;
                destinationLine.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (debugMaterial != null)
            {
                Destroy(debugMaterial);
            }
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            if (pathLine != null)
            {
                pathLine.startColor = pathColor;
                pathLine.endColor = pathColor;
            }
        }
    }
}
