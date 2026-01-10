using System.Collections.Generic;
using UnityEngine;

namespace SimpleSkills
{
    public struct PathResult
    {
        public bool DidFindPath;
        public List<Vector2Int> ResultPath;
        public float PathLength;
    }
}
