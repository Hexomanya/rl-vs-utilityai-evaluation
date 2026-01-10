using System;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleSkills
{
    public struct AStarNode : IComparable<AStarNode>
    {
        public int2 position;
        public float gCost;
        public float hCost;
        public float fCost;

        public int CompareTo(AStarNode other)
        {
            int compare = fCost.CompareTo(other.fCost);
            if(compare == 0) compare = hCost.CompareTo(other.hCost);
            
            return compare;
        }
    }
}
