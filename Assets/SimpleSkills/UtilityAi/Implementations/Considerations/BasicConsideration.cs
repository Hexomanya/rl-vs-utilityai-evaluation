using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "BasicConsideration", menuName = "UtilityAi/Considerations/BasicConsideration")]
    public class BasicConsideration : Consideration
    {
        public override float Evaluate(ISkAgent currentAgent, WorldState worldState)
        {
            float value = worldState.GetData(_valueKey, 0f);
            return _animationCurve.Evaluate(value);
        }
    }
}
