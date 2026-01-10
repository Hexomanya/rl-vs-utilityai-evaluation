using UnityEngine;

namespace _General.TypeExtensions
{
    public static class Vector2IntExtension
    {
        public static float SquaredDistance(this Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            return dx * dx + dy * dy;
        }
    }
}
