using System;

namespace MMOTableGame.Hexes.Navigation
{
    public readonly struct HexTileKey : IEquatable<HexTileKey>
    {
        public HexCoordinates Coordinates { get; }
        public int Layer { get; }

        public HexTileKey(HexCoordinates coordinates, int layer)
        {
            Coordinates = coordinates;
            Layer = layer;
        }

        public bool Equals(HexTileKey other)
        {
            return Coordinates == other.Coordinates && Layer == other.Layer;
        }

        public override bool Equals(object obj)
        {
            return obj is HexTileKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Coordinates, Layer);
        }

        public override string ToString()
        {
            return $"{Coordinates} L{Layer}";
        }
    }
}
