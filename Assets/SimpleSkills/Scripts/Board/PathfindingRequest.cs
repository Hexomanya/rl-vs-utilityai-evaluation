using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleSkills
{
    public class PathfindingRequest
    {
        public int2 OriginPosition { get; private set; } 
        public int2 TargetPosition { get; private set; } 
        public bool MoveNextToTarget { get; private set; }
        
        public int ElapsedTimeMs { get; private set; }
        
        public bool IsDone { get; private set; }
        public PathResult Result { get; private set; }

        private readonly SkBoardManager _boardManager;
        
        public PathfindingRequest(Vector2Int origin, Vector2Int target, bool moveNextToTarget, SkBoardManager boardManager)
        {
            this.OriginPosition = new int2(origin.x, origin.y);
            this.TargetPosition = new int2(target.x, target.y);
            this.MoveNextToTarget = moveNextToTarget;
            _boardManager = boardManager;
        }

        public void AddElapsedTime(int addValue)
        {
            this.ElapsedTimeMs += addValue;
        }

        public void OnIsDone(NativeList<int2> path, bool didFindPath, bool didAbort)
        {
            List<Vector2Int> foundPath = new List<Vector2Int>();
            
            for (int i = 0; i < path.Length; i++)
            {
                foundPath.Add(new Vector2Int(path[i].x, path[i].y));
            }
            
            float length = 0;
            for (int i = 0; i < foundPath.Count - 1; i++)
            {
                length += _boardManager.TileToWorldDistance(foundPath[i], foundPath[i + 1]);
            }

            this.Result = new PathResult {
                DidFindPath = didFindPath, 
                PathLength = length, 
                ResultPath = foundPath,
            };

            this.IsDone = !didAbort;
        }
    }
}
