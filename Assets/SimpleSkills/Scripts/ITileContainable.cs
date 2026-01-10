using UnityEngine;

namespace SimpleSkills
{
    public interface ITileContainable
    {
        public Color TileColor { get; }
        public bool IsObstruction { get; }
    }
}
