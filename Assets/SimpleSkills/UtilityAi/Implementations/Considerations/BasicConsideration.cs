using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "BasicConsideration", menuName = "UtilityAi/Considerations/BasicConsideration")]
    public class BasicConsideration : Consideration
    {
        [SerializeField] protected Aggregator _keyOverrideValue;
        
        public override float Evaluate(ISkAgent currentAgent, WorldState worldState)
        {
            float value = _keyOverrideValue is not null ? 
                Mathf.Clamp01(_keyOverrideValue.Evaluate(currentAgent, worldState)) : 
                worldState.GetData(_valueKey, 0f);
       
            return _animationCurve.Evaluate(value);
        }
    }
}
