using UnityEngine;

namespace MMOTableGame.Hexes
{
    [DisallowMultipleComponent]
    public sealed class HexTileInstance : MonoBehaviour
    {
        [SerializeField, HideInInspector] private int q;
        [SerializeField, HideInInspector] private int r;
        [SerializeField, HideInInspector] private int layer;

        public HexCoordinates Coordinates => new(q, r);
        public int Layer => layer;

        public void SetPlacement(HexCoordinates coordinates, int placementLayer)
        {
            q = coordinates.Q;
            r = coordinates.R;
            layer = placementLayer;
        }
    }
}
