using System;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleRangedFireAttack", menuName = "SimpleSkills/SimpleRangedFireAttack")]
    public class SimpleRangedFireAttack : SimpleSkill
    {
        [SerializeField] private int _damage = 10;
        [SerializeReference] private Element _requiredElementType;
        [SerializeField] private int _resourceChargeCost = 2;

        public override int ID { get => (int)SkillIndex.RangedFireAttack; }
        
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
            
            // float distance = boardManager.TileToWorldDistance(originPos, targetPos);
            // if(distance > this.Range) return false;
            
            Resource resource = boardManager.GetResourceInRadius(originPos, _requiredElementType.GetType(), 1);
            if(resource == null) return Task.FromResult(false);
            if(resource.Charges < _resourceChargeCost) return Task.FromResult(false);
            
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
            
            //TODO: Should be in CanExecute but we need the resource reference. Clean up
            Resource resource = boardManager.GetResourceInRadius(originPos, _requiredElementType.GetType(), 1);
            if(resource == null) return Task.FromResult(false);
            if(resource.Charges < _resourceChargeCost) return Task.FromResult(false);
            
            //TODO: If game gets more complex we might allow to shoot non agent tile.
            ISkAgent targetAgent = boardManager.GetAgentOnTileOrInRange(targetPos.Value, originPos, this.Range);
            if(targetAgent is null) return Task.FromResult(false);
            
            bool hasLineOfSight = boardManager.HasLineOfSight(originPos, targetPos.Value);
            if(!hasLineOfSight) return Task.FromResult(false); //TODO: Is Shooting a wall a legitimate use of the action?
            
            context.OriginAgent.DealDamage(targetAgent, _damage);
            resource.ReduceCharges(_resourceChargeCost);
            return Task.FromResult(true);
        }
    }
}
