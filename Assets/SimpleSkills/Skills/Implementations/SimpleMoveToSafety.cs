using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleMoveToSafety", menuName = "SimpleSkills/SimpleMoveToSafety")]
    public class SimpleMoveToSafety : SimpleSkill
    {
        public override int ID { get => -1; }
        public override async Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken)
        {
            SkGameplayManager gameplayManager = context.OriginAgent.GameplayManager;
            
            ValueMap safetyMap = gameplayManager.BoardManager.ComputeSafetyMap(context.OriginAgent);
            if(safetyMap.Values.Length <= 0)
            {
                Debug.LogError("The safety map was not initiallized correctly!");
                return false;
            }
            
            Vector2Int? safestReachablePosition = await gameplayManager.BoardManager.GetClosestReachableSafePosition(safetyMap, context.OriginAgent, cancelToken);
            if(safestReachablePosition is null)
            {
                Debug.LogWarning("Could not find a safest reachable position!");
                return false;
            }

            if(safestReachablePosition == context.OriginAgent.Position)
            {
                //Debug.Log("Agent already is on the safest position");
                return false;
            }

            context.TargetPosition = safestReachablePosition.Value;
            context.IsValidated = true;
            
            return true;
        }
        public override async Task<bool> ExecuteSkill(SkillContext context, CancellationToken cancelToken)
        {
            if(!context.IsValidated)
            {
                Debug.LogWarning("Skills should be validated before executing them!");
                bool didValidate = await this.CanExecute(context, cancelToken);
                if(!didValidate) return false;
            }
            
            if(context.TargetPosition is null)
            {
                Debug.LogError("TargetPosition can not be null!");
                return false;
            }
            
            context.OriginAgent.Position = context.TargetPosition.Value;
            return true;
        }
    }
}
