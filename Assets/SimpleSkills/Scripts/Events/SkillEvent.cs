using _General;
using UnityEngine;

namespace SimpleSkills.Scripts
{
    [CreateAssetMenu(fileName = "SkillEvent", menuName = "Events/SkillEvent")]
    public class SkillEvent : ScriptableEvent<SimpleSkill> {}
}
