using UnityEngine;

namespace SimpleSkills
{
    public class Wall : ITileContainable
    {
        public Color TileColor { get => Color.black; }
        public bool IsObstruction { get => true; }
    }
}
