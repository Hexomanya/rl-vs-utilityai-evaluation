using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "ConstantConsideration", menuName = "UtilityAi/Considerations/ConstantConsideration")]
    public class ConstantConsideration : Consideration
    {
        [SerializeField] private float _constantValue = 1f;
        
        public override float Evaluate(ISkAgent currentAgent, WorldState worldState)
        {
            return _constantValue;
        }
    }
}
