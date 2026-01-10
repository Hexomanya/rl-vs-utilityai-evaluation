using _General;
using UnityEngine;

namespace SimpleSkills.Scripts
{
    [CreateAssetMenu(fileName = "SkAgentEvent", menuName = "Events/SkAgentEvent")]
    public class SkAgentEvent : ScriptableEvent<ISkAgent> {}
}
