using UnityEngine;

namespace MMOTableGame.Hexes.Navigation
{
    public sealed class HexNavigationNode
    {
        public HexTileInstance Tile { get; }
        public HexTileKey Key { get; }
        public HexCoordinates Coordinates => Key.Coordinates;
        public int Layer => Key.Layer;
        public Vector3 WorldPosition { get; }

        public HexNavigationNode(HexTileInstance tile, Vector3 worldPosition)
        {
            Tile = tile;
            Key = new HexTileKey(tile.Coordinates, tile.Layer);
            WorldPosition = worldPosition;
        }
    }
}
