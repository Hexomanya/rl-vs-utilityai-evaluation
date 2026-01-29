using System.Threading;
using System.Threading.Tasks;
using SimpleSkills.Scripts;
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
            //TODO: Only allow attack on selcted position
            Vector2Int originPos = context.OriginAgent.Position;
            Vector2Int? targetPos = context.TargetPosition;
            SkBoardManager boardManager = context.OriginAgent.GameplayManager.BoardManager;
            
            if(targetPos is null)
            {
                Debug.LogError("TargetPosition can not be null!");
                return Task.FromResult(false);
            }

            if(targetPos == originPos)
            {
                GameLog.Print("Can not target self!");
                return Task.FromResult(false);
            }

            SkTileManager tile = boardManager.GetTileAt(targetPos.Value);

            if(tile is null)
            {
                Debug.LogError("Skill was not provided a valid target position!");
                return Task.FromResult(false);
            }

            if(tile.ContainedEntity is not ISkAgent targetAgent)
            {
                GameLog.Print("Selected tile does not contain a target!");
                return Task.FromResult(false);
            }
            
            bool hasLineOfSight = boardManager.HasLineOfSight(originPos, targetPos.Value);

            if(!hasLineOfSight)
            {
                GameLog.Print("You don't have line of sight!");
                return Task.FromResult(false);
            }

            context.TargetAgents = new[] { targetAgent };
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
    }
}
