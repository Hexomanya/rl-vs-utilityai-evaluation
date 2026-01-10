using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleMoveIntoMelee", menuName = "SimpleSkills/SimpleMoveIntoMelee")]
    public class SimpleMoveIntoMelee : SimpleSkill
    {
        public override int ID { get => -1; }
        
        public override async Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken)
        {
            SkGameplayManager gameplayManager = context.OriginAgent.GameplayManager;
            
            SkBoardManager board = context.OriginAgent.GameplayManager.BoardManager;
            List<SkTileManager> enemyTiles = board.GetTilesInRadiusWithQuery(context.OriginAgent.Position, 1, tile => this.IsEnemyTile(tile, context.OriginAgent));
            List<ISkAgent> agents = enemyTiles.Select(tile => tile.ContainedEntity as ISkAgent).ToList();
            bool alreadyInMelee = agents.Count > 0;
            if(alreadyInMelee) return false;
            
            PathToTarget pathToClosestAgent = await gameplayManager.GetClosestEnemyAgent(context.OriginAgent.Position, new List<int>{context.OriginAgent.FactionIndex}, cancelToken);

            //Did not find path
            if(pathToClosestAgent.Target is null)
            {
                //Debug.Log("Pathfinding CanExecute failed in turn: " + context.TurnId);
                return false;
            }
            if(pathToClosestAgent.Target.ID.Equals(context.OriginAgent.ID))
            {
                Debug.LogWarning("Found enemy is caller! That is not valid behaviour here!");
                return false;
            }
            
            Vector2Int newPosition = gameplayManager.BoardManager.GetMaximumReachableTargetPosition(context.OriginAgent, pathToClosestAgent.PathDescription);
            
            //Tried to move but already at best position
            if(newPosition.Equals(context.OriginAgent.Position))
            {
                //Debug.LogWarning("Something went wrong when getting reachable position");
                return false;
            }
            
            context.TargetPosition = newPosition;
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
