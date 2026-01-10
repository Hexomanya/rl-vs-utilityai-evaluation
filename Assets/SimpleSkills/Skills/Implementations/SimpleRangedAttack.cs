using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleRangedAttack", menuName = "SimpleSkills/SimpleRangedAttack")]
    public class SimpleRangedAttack : SimpleSkill
    {
        [SerializeField] private int _damage = 2; 
        public override int ID { get => (int)SkillIndex.RangedAttack; }
        
        public override Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken)
        {
            Vector2Int originPos = context.OriginAgent.Position;
            Vector2Int? targetPos = context.TargetPosition;
            SkBoardManager boardManager = context.OriginAgent.GameplayManager.BoardManager;
            
            if(targetPos is null)
            {
                Debug.LogError("TargetPosition can not be null!");
                return Task.FromResult(false);
            }
            
            ISkAgent targetAgent = boardManager.GetAgentOnTileOrInRange(targetPos.Value, originPos, this.Range);
            if(targetAgent is null) return Task.FromResult(false);
            
            bool hasLineOfSight = boardManager.HasLineOfSight(originPos, targetPos.Value);
            if(!hasLineOfSight) return Task.FromResult(false);
            
            return Task.FromResult(true);
        }

        public override Task<bool> ExecuteSkill(SkillContext context, CancellationToken cancelToken)
        {
            Vector2Int originPos = context.OriginAgent.Position;
            Vector2Int? targetPos = context.TargetPosition;
            SkBoardManager boardManager = context.OriginAgent.GameplayManager.BoardManager;
            
            if(targetPos is null)
            {
                Debug.LogError("TargetPosition can not be null!");
                return Task.FromResult(false);
            }
            
            ISkAgent targetAgent = boardManager.GetAgentOnTileOrInRange(targetPos.Value, originPos, this.Range);
            if(targetAgent is null) return Task.FromResult(false);
            
            bool hasLineOfSight = boardManager.HasLineOfSight(originPos, targetPos.Value);
            if(!hasLineOfSight) return Task.FromResult(false); //TODO: Is Shooting a wall a legitimate use of the action?
            
            context.OriginAgent.DealDamage(targetAgent, _damage);
            return Task.FromResult(true);
        }
    }
}
