using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "UtilityAction", menuName = "UtilityAi/Actions/UtilityAction")]
    public class UtilityAction : ScriptableObject
    {
        [SerializeField] protected SimpleSkill _skill;
        [SerializeField] protected Aggregator _finalAggregator;

        public SimpleSkill Skill { get => _skill; }

        public virtual void ExecuteSkill(SkillContext context, WorldState worldState, CancellationToken cancelToken)
        {
            _skill.ExecuteSkill(context, cancelToken);
        }
        
        public virtual float GetUtilityValue(ISkAgent agent, WorldState worldState)
        {
            return _finalAggregator?.Evaluate(agent, worldState) ?? 0;
        }
        
        public Vector2Int GetLastPosition()
        {
            throw new System.NotImplementedException();
        }
    }
}
