using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SimpleSkills.UtilityAi.Implementations.Actions
{
    [CreateAssetMenu(fileName = "MoveIntoMeleeAction", menuName = "UtilityAi/Actions/MoveIntoMeleeAction")]
    public class MoveIntoMeleeAction : UtilityAction
    {
        public override void ExecuteSkill(SkillContext context, WorldState worldState,  CancellationToken cancelToken)
        {
            _skill.ExecuteSkill(context, cancelToken);
        }
        
        public override float GetUtilityValue(ISkAgent agent, WorldState worldState)
        {
            return _finalAggregator?.Evaluate(agent, worldState) ?? 0;
        }
    }
}
