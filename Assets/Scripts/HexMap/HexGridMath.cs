using UnityEngine;

namespace MMOTableGame.Hexes
{
    public static class HexGridMath
    {
        public const float Sqrt3 = 1.7320508075688772f;

        public static Vector3 CoordinatesToLocalPosition(HexCoordinates coordinates, float radius)
        {
            float x = radius * 1.5f * coordinates.Q;
            float z = radius * Sqrt3 * (coordinates.R + coordinates.Q * 0.5f);
            return new Vector3(x, 0f, z);
        }

        public static HexCoordinates LocalPositionToCoordinates(Vector3 localPosition, float radius)
        {
            float q = (2f / 3f * localPosition.x) / radius;
            float r = (-1f / 3f * localPosition.x + Sqrt3 / 3f * localPosition.z) / radius;
            return RoundAxial(q, r);
        }

        public static bool IsInsideRadius(HexCoordinates coordinates, int gridRadius)
        {
            return Mathf.Max(
                Mathf.Abs(coordinates.Q),
                Mathf.Abs(coordinates.R),
                Mathf.Abs(coordinates.S)) <= gridRadius;
        }

        public static Vector3 Corner(Vector3 center, float radius, int corner)
        {
            float angle = Mathf.Deg2Rad * (60f * corner);
            return center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static HexCoordinates RoundAxial(float q, float r)
        {
            float s = -q - r;
            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            int roundedS = Mathf.RoundToInt(s);

            float qDifference = Mathf.Abs(roundedQ - q);
            float rDifference = Mathf.Abs(roundedR - r);
            float sDifference = Mathf.Abs(roundedS - s);

            if (qDifference > rDifference && qDifference > sDifference)
            {
                roundedQ = -roundedR - roundedS;
            }
            else if (rDifference > sDifference)
            {
                roundedR = -roundedQ - roundedS;
            }

            return new HexCoordinates(roundedQ, roundedR);
        }
    }
}
