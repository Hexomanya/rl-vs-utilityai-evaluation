using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleStupidRangedAttack", menuName = "SimpleSkills/SimpleStupidRangedAttack")]
    public class SimpleStupidRangedAttack : SimpleSkill
    {
        [SerializeField] private int _damage = 2; 
        public override int ID { get => (int)SkillIndex.RangedAttack; }
        
        public override Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken,  bool isMaskingCall = false)
        {
            Vector2Int originPos = context.OriginAgent.Position;
            SkBoardManager boardManager = context.OriginAgent.GameplayManager.BoardManager;

            ISkAgent nearestValidEnemy = context.OriginAgent.GameplayManager.BoardManager
                .GetTilesInRadiusWithQuery(
                    context.OriginAgent.Position,
                    boardManager.WorldToTileDistance(this.Range),
                    tileManager => this.IsEnemyTile(tileManager, context.OriginAgent))
                .Select(manager => manager.ContainedEntity as ISkAgent)
                .FirstOrDefault(agent => this.IsValidTarget(agent, originPos, boardManager));
            
            if(nearestValidEnemy == null) return Task.FromResult(false);
            
            context.TargetAgents = new List<ISkAgent>{nearestValidEnemy};
            context.IsValidated = true;
            return Task.FromResult(true);
        }

        public override Task<bool> ExecuteSkill(SkillContext context, CancellationToken cancelToken)
        {
            if(!context.IsValidated)
            {
                Debug.LogWarning("Skills should be validated before executing them!");
                bool didValidate = this.CanExecute(context, cancelToken).Result;
                if(!didValidate) return Task.FromResult(false);
            }
            
            if(context.TargetAgents.Count <= 0) Debug.LogError("TargetAgents should not be empty after being validated!");
            
            context.OriginAgent.DealDamage(context.TargetAgents[0], _damage);
            return Task.FromResult(true);
        }

        private bool IsValidTarget(ISkAgent agent, Vector2Int originPos, SkBoardManager boardManager)
        {
            float distance = boardManager.TileToWorldDistance(originPos, agent.Position);
            if (distance > this.Range) return false;

            return boardManager.HasLineOfSight(originPos, agent.Position);
        }
    }
}
