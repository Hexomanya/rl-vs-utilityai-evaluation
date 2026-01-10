using System;
using UnityEngine;

namespace _General
{
    public static class OneDimUtil
    {
        /// <summary>Converts 2D coordinates to 1D array index</summary>
        public static int GetIndex(int x, int y, int width)
        {
            if (width <= 0) throw new ArgumentException("Width must be positive", nameof(width));
            if (x < 0 || y < 0) throw new ArgumentException("Coordinates must be non-negative");
            return x + y * width;
        }

        /// <summary>Converts 2D coordinates to 1D array index</summary>
        public static int GetIndex(Vector2Int position, int width)
        {
            return OneDimUtil.GetIndex(position.x, position.y, width);
        }
        
        /// <summary>Converts 1D array index to 2D coordinates</summary>
        public static Vector2Int GetPosition(int index, int width, int height)
        {
            if (index >= width * height) throw new ArgumentOutOfRangeException(nameof(index));
            return new Vector2Int(index % width, index / width);
        }
        
        /// <summary>Converts 1D array index to 2D coordinates, without safty checks</summary>
        public static Vector2Int GetPositionUnsafe(int index, int width)
        {
            return new Vector2Int(index % width, index / width);
        }
    }
}
