using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleMove", menuName = "SimpleSkills/SimpleMove")]
    public class SimpleMove : SimpleSkill
    {
        public override int ID { get => (int)SkillIndex.Move; }
        
        public override async Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken)
        {
            // Masking has no targetPosition, so we just return true
            if(context.TargetPosition is null) return true;
            
            Vector2Int targetPos = context.TargetPosition.Value;
            if(targetPos == context.OriginAgent.Position) return false;
            
            SkBoardManager boardManager = context.OriginAgent.GameplayManager.BoardManager;
            SkTileManager tile = boardManager.GetTileAt(targetPos);

            if(tile is null)
            {
                Debug.Log($"{context.OriginAgent.GetName()} tried to move to tile that should be masked at {targetPos}!");
                return false;
            }
            
            if(!tile.IsWalkable()) return false;

            PathfindingRequest moveRequest = new PathfindingRequest(context.OriginAgent.Position, targetPos, false, boardManager);
            await boardManager.GetPath(moveRequest, cancelToken);

            if(!moveRequest.Result.DidFindPath) return false;
            
            Vector2Int newPosition = boardManager.GetMaximumReachableTargetPosition(context.OriginAgent, moveRequest.Result);
            
            if(newPosition.Equals(context.OriginAgent.Position))
            {
                Debug.LogWarning($"Tried to move to same position! Position is: {context.OriginAgent.Position}, ClickedPosition is: {context.TargetPosition}");
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
