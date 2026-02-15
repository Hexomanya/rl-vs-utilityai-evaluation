using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleMeleeAttack", menuName = "SimpleSkills/SimpleMeleeAttack")]
    public class SimpleMeleeAttack : SimpleSkill
    {
        [SerializeField] private int _damage = 4;
        
        public override int ID { get => (int)SkillIndex.MeleeAttack; }

        public override Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken,  bool isMaskingCall = false)
        {
            Vector2Int center = context.OriginAgent.Position;
            SkBoardManager board = context.OriginAgent.GameplayManager.BoardManager;
            List<SkTileManager> tiles = board.GetTilesInRadiusWithQuery(center, 1, tile => this.IsEnemyTile(tile, context.OriginAgent));
            List<ISkAgent> agents = tiles.Select(tile => tile.ContainedEntity as ISkAgent).ToList();

            context.TargetAgents = agents;
            context.IsValidated = agents.Count > 0;
            return Task.FromResult(context.IsValidated);
        }

        public override Task<bool> ExecuteSkill(SkillContext context, CancellationToken cancelToken)
        {
            if(!context.IsValidated)
            {
                Debug.LogWarning("Skills should be validated before executing them!");
                bool didValidate = this.CanExecute(context, cancelToken).Result;
                if(!didValidate) return Task.FromResult(false);
            }
     
            foreach (ISkAgent targetAgent in context.TargetAgents)
            {
                context.OriginAgent.DealDamage(targetAgent, _damage);
            }

            return Task.FromResult(true);
        }
    }
}
