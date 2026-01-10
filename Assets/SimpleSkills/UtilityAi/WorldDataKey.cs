using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "WorldDataKey", menuName = "UtilityAi/WorldDataKey")]
    public class WorldDataKey : ScriptableObject
    {
        [SerializeField] private string _description;
    }
}
