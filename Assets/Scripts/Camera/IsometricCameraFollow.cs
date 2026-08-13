using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace MMOTableGame.CameraSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraFollow : MonoBehaviour
    {
        private const float IsometricPitch = 35.2643897f;

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 focusOffset = new(0f, 0.5f, 0f);

        [Header("Isometric View")]
        [SerializeField, Range(10f, 80f)] private float pitch = IsometricPitch;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(1f)] private float distance = 20f;
        [SerializeField, Min(0.1f)] private float orthographicSize = 8f;

        [Header("Follow")]
        [SerializeField, Min(0f)] private float smoothTime = 0.08f;

        [Header("Edge Pan")]
        [SerializeField] private bool edgePanEnabled = true;
        [SerializeField, Min(1f)] private float edgeThickness = 20f;
        [SerializeField, Min(0.01f)] private float edgePanSpeed = 6f;
        [Tooltip("Distancia máxima desde el Player. Un valor de 0 permite recorrer el mapa sin límite.")]
        [SerializeField, Min(0f)] private float maximumPanDistance;
        [FormerlySerializedAs("recenterWithHomeKey")]
        [SerializeField] private bool recenterWithSpaceKey = true;

        private Camera targetCamera;
        private Vector3 followVelocity;
        private Vector3 panOffset;
        private bool pointerReadyForEdgePan;

        public Transform Target => target;
        public Vector3 PanOffset => panOffset;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ConfigureProjection();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ConfigureProjection();
            UpdateEdgePan();

            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = target.position + focusOffset + panOffset;
            Vector3 desiredPosition = focusPoint - viewRotation * Vector3.forward * distance;

            transform.rotation = viewRotation;
            transform.position = smoothTime <= 0f
                ? desiredPosition
                : Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref followVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    Time.deltaTime);
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            panOffset = Vector3.zero;
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = target.position + focusOffset;
            transform.SetPositionAndRotation(
                focusPoint - viewRotation * Vector3.forward * distance,
                viewRotation);
            followVelocity = Vector3.zero;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        public void ResetPanOffset()
        {
            panOffset = Vector3.zero;
            followVelocity = Vector3.zero;
        }

        public void TickEdgePan(Vector2 pointerPosition, Vector2 viewportSize, float deltaTime)
        {
            if (!edgePanEnabled || deltaTime <= 0f || viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                return;
            }

            if (pointerPosition.x < 0f || pointerPosition.y < 0f ||
                pointerPosition.x > viewportSize.x || pointerPosition.y > viewportSize.y)
            {
                return;
            }

            float horizontalEdge = Mathf.Min(edgeThickness, viewportSize.x * 0.5f);
            float verticalEdge = Mathf.Min(edgeThickness, viewportSize.y * 0.5f);
            Vector2 panInput = Vector2.zero;

            if (pointerPosition.x <= horizontalEdge)
            {
                panInput.x = -1f;
            }
            else if (pointerPosition.x >= viewportSize.x - horizontalEdge)
            {
                panInput.x = 1f;
            }

            if (pointerPosition.y <= verticalEdge)
            {
                panInput.y = -1f;
            }
            else if (pointerPosition.y >= viewportSize.y - verticalEdge)
            {
                panInput.y = 1f;
            }

            Pan(panInput, deltaTime);
        }

        public void Pan(Vector2 screenDirection, float deltaTime)
        {
            if (screenDirection.sqrMagnitude <= 0f || deltaTime <= 0f)
            {
                return;
            }

            screenDirection = Vector2.ClampMagnitude(screenDirection, 1f);
            Quaternion horizontalRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 worldDirection =
                horizontalRotation * Vector3.right * screenDirection.x +
                horizontalRotation * Vector3.forward * screenDirection.y;
            worldDirection = Vector3.ClampMagnitude(worldDirection, 1f);

            panOffset += worldDirection * (edgePanSpeed * deltaTime);
            panOffset.y = 0f;
            if (maximumPanDistance > 0f)
            {
                panOffset = Vector3.ClampMagnitude(panOffset, maximumPanDistance);
            }
        }

        private void UpdateEdgePan()
        {
            if (recenterWithSpaceKey && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ResetPanOffset();
                return;
            }

            if (Mouse.current == null || !Application.isFocused)
            {
                pointerReadyForEdgePan = false;
                return;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Vector2 viewportSize = new(Screen.width, Screen.height);
            if (!pointerReadyForEdgePan)
            {
                pointerReadyForEdgePan = IsPointerInsideSafeArea(pointerPosition, viewportSize);
                return;
            }

            TickEdgePan(
                pointerPosition,
                viewportSize,
                Time.unscaledDeltaTime);
        }

        private bool IsPointerInsideSafeArea(Vector2 pointerPosition, Vector2 viewportSize)
        {
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                return false;
            }

            float horizontalEdge = Mathf.Min(edgeThickness, viewportSize.x * 0.5f);
            float verticalEdge = Mathf.Min(edgeThickness, viewportSize.y * 0.5f);
            return pointerPosition.x > horizontalEdge &&
                   pointerPosition.x < viewportSize.x - horizontalEdge &&
                   pointerPosition.y > verticalEdge &&
                   pointerPosition.y < viewportSize.y - verticalEdge;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                pointerReadyForEdgePan = false;
            }
        }

        private void ConfigureProjection()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = orthographicSize;
        }

        private void OnValidate()
        {
            distance = Mathf.Max(1f, distance);
            orthographicSize = Mathf.Max(0.1f, orthographicSize);
            smoothTime = Mathf.Max(0f, smoothTime);
            edgeThickness = Mathf.Max(1f, edgeThickness);
            edgePanSpeed = Mathf.Max(0.01f, edgePanSpeed);
            maximumPanDistance = Mathf.Max(0f, maximumPanDistance);
        }
    }
}
