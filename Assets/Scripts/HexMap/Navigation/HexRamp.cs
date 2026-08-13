using UnityEngine;

namespace MMOTableGame.Hexes.Navigation
{
    [DisallowMultipleComponent]
    public sealed class HexRamp : MonoBehaviour
    {
        private static readonly HexCoordinates[] Directions =
        {
            new(1, 0),
            new(1, -1),
            new(0, -1),
            new(-1, 0),
            new(-1, 1),
            new(0, 1)
        };

        [Tooltip("Dirección axial hacia la parte alta de la rampa.")]
        [SerializeField, Range(0, 5)] private int uphillDirection = 2;
        [Tooltip("Cantidad de layers que conecta la rampa.")]
        [SerializeField, Range(1, 10)] private int layerDelta = 1;
        [Tooltip("Altura local de la superficie en el extremo bajo del modelo.")]
        [SerializeField, Min(0f)] private float lowSurfaceHeight = 1f;
        [Tooltip("Altura local de la superficie en el extremo alto del modelo.")]
        [SerializeField, Min(0f)] private float highSurfaceHeight = 2f;

        public int UphillDirection => Mathf.Clamp(uphillDirection, 0, Directions.Length - 1);
        public HexCoordinates UphillOffset => Directions[UphillDirection];
        public int LayerDelta => Mathf.Max(1, layerDelta);

        public void Configure(
            int direction,
            int connectedLayerDelta = 1,
            float lowHeight = 1f,
            float highHeight = 2f)
        {
            uphillDirection = Mathf.Clamp(direction, 0, Directions.Length - 1);
            layerDelta = Mathf.Max(1, connectedLayerDelta);
            lowSurfaceHeight = Mathf.Max(0f, lowHeight);
            highSurfaceHeight = Mathf.Max(lowSurfaceHeight, highHeight);
        }

        public HexCoordinates GetDownhillCoordinates(HexCoordinates rampCoordinates)
        {
            HexCoordinates offset = UphillOffset;
            return new HexCoordinates(rampCoordinates.Q - offset.Q, rampCoordinates.R - offset.R);
        }

        public HexCoordinates GetUphillCoordinates(HexCoordinates rampCoordinates)
        {
            HexCoordinates offset = UphillOffset;
            return new HexCoordinates(rampCoordinates.Q + offset.Q, rampCoordinates.R + offset.R);
        }

        public Vector3 GetNavigationPosition()
        {
            float middleHeight = (lowSurfaceHeight + highSurfaceHeight) * 0.5f;
            return transform.TransformPoint(Vector3.up * middleHeight);
        }

        private void OnValidate()
        {
            uphillDirection = Mathf.Clamp(uphillDirection, 0, Directions.Length - 1);
            layerDelta = Mathf.Max(1, layerDelta);
            lowSurfaceHeight = Mathf.Max(0f, lowSurfaceHeight);
            highSurfaceHeight = Mathf.Max(lowSurfaceHeight, highSurfaceHeight);
        }
    }
}
