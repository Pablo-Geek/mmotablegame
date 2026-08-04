using System;
using UnityEngine;

namespace MMOTableGame.Hexes
{
    [Serializable]
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        [SerializeField] private readonly int q;
        [SerializeField] private readonly int r;

        public int Q => q;
        public int R => r;
        public int S => -q - r;

        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public bool Equals(HexCoordinates other) => q == other.q && r == other.r;

        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(q, r);

        public override string ToString() => $"({q}, {r})";

        public static bool operator ==(HexCoordinates left, HexCoordinates right) => left.Equals(right);

        public static bool operator !=(HexCoordinates left, HexCoordinates right) => !left.Equals(right);
    }
}
