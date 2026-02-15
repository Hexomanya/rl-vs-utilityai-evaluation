using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    
    [CreateAssetMenu(fileName = "ValueInputConsideration", menuName = "UtilityAi/Considerations/ValueInputConsideration")]
    public class ValueInputConsideration : Consideration
    {
        public override float Evaluate(ISkAgent currentAgent, WorldState worldState)
        {
            return worldState.GetData(_valueKey, 0f);
        }
    }
}
