using _General;
using UnityEngine;

namespace SimpleSkills.Scripts
{
    [CreateAssetMenu(fileName = "WorldPositionEvent", menuName = "Events/WorldPositionEvent")]
    public class WorldPositionEvent : ScriptableEvent<Vector3> {}
}
