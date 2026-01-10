using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "ActionPointConsideration", menuName = "UtilityAi/Considerations/ActionPointConsideration")]
    public class ActionPointConsideration : Consideration
    {
        public override float Evaluate(ISkAgent currentAgent, WorldState worldState)
        {
            float actionPoints = worldState.GetData(_valueKey, 0);

            if(actionPoints > GameConfig.ActionPointMax)
            {
                actionPoints = Mathf.Min(GameConfig.ActionPointMax, actionPoints);
                Debug.LogWarning("Needed to clamp action points!");
            }

            return _animationCurve.Evaluate(actionPoints / GameConfig.ActionPointMax);
        }
    }
}
