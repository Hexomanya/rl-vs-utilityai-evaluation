using System;
using UnityEngine;

namespace SimpleSkills
{
    [Serializable]
    public struct Bounds2Int
    {
        public int StartIndex;
        public int Width;
        public int Height;

        public Bounds2Int(int startIndex, int width, int height)
        {
            this.StartIndex = startIndex;
            this.Width = width;
            this.Height = height;
        }
    }
}
