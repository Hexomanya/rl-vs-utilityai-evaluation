using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SimpleSkills
{
    // https://docs.unity3d.com/6000.0/Documentation/Manual/job-system-creating-jobs.html
    // https://docs.unity3d.com/6000.0/Documentation/Manual/job-system-native-container.html
    // https://docs.unity3d.com/Packages/com.unity.burst@1.2/manual/index.html
    // https://www.geeksforgeeks.org/dsa/a-search-algorithm/
    [BurstCompile]
    public struct AStarPathFinder : IJob
    {
        [ReadOnly] public int2 startPosition;
        [ReadOnly] public int2 targetPosition;
        [ReadOnly] public int2 boardSize;
        [ReadOnly] public bool moveNextToTarget;
        
        [ReadOnly] public NativeArray<bool> walkabilityMap;
        public NativeList<int2> resultPath;
        public NativeReference<bool> pathFound;
        
        // public NativeReference<int> debugMessage;
        // public NativeReference<int> iterationCount;
        
        //TODO: Docs say, job should complete in one frame, so we should probably split this monolith up
        public void Execute()
        {
            resultPath.Clear();
            pathFound.Value = false;
           
            if (!this.IsValidPosition(startPosition) || !this.IsValidPosition(targetPosition)) return;
            if (!moveNextToTarget && !walkabilityMap[this.GetIndex(targetPosition)]) return; //Target position is not walkable
            
            NativeList<AStarNode> openNodes = new NativeList<AStarNode>(Allocator.Temp);
            NativeHashMap<int, float> closedNodes = new NativeHashMap<int, float>(boardSize.x * boardSize.y, Allocator.Temp);
            NativeHashMap<int, int> cameFrom = new NativeHashMap<int, int>(boardSize.x * boardSize.y, Allocator.Temp);
           
            AStarNode startNode = new AStarNode {
                position = startPosition,
                gCost = 0,
                hCost = AStarPathFinder.Heuristic(startPosition, targetPosition),
            };
            
            startNode.fCost = startNode.gCost + startNode.hCost;
            openNodes.Add(startNode);
            
            while (openNodes.Length > 0)
            {
                // Find lowest f cost
                int currentIndexOpen = 0;
                for (int i = 1; i < openNodes.Length; i++)
                {
                    if(openNodes[i].fCost < openNodes[currentIndexOpen].fCost)
                    {
                        currentIndexOpen = i;
                    }
                }

                // Remove from open
                AStarNode currentNode = openNodes[currentIndexOpen];
                openNodes.RemoveAtSwapBack(currentIndexOpen);
                
                int currentIndex = this.GetIndex(currentNode.position);
                
                if(closedNodes.TryGetValue(currentIndex, out float closedCost))
                {
                    if(closedCost <= currentNode.fCost)
                    {
                        // Already processed with equal or better cost, skip
                        continue;
                    }
                }
                

                // Check if at goal
                if(this.IsAtTargetPosition(currentNode.position))
                {
                    this.ReconstructPath(cameFrom, currentIndex);
                    pathFound.Value = true;

                    openNodes.Dispose();
                    closedNodes.Dispose();
                    cameFrom.Dispose();
                    return;
                }
                
                // Add to closed set (or update if worse cost was there)
                closedNodes[currentIndex] = currentNode.fCost;
                
                // Handle Children
                NativeArray<int2> childPositions = AStarPathFinder.GetChildPositions(currentNode.position);

                for (int i = 0; i < childPositions.Length; i++)
                {
                    int2 childPosition = childPositions[i];
                    
                    if(!this.IsValidPosition(childPosition) ||
                       !walkabilityMap[this.GetIndex(childPosition)]
                    ) continue;

                    int childIndex = this.GetIndex(childPosition);

                    float childGCost = currentNode.gCost + this.GetMoveToParentCost(currentNode.position, childPosition);
                    float childHCost = AStarPathFinder.Heuristic(childPosition, targetPosition);
                    float childFCost = childGCost + childHCost;
                    
                    if(closedNodes.TryGetValue(childIndex, out float closedSetFCost))
                    {
                        if(closedSetFCost <= childFCost) continue;
                    }
                    
                    // Check if cheaper version already exists in open set
                    float openSetFCost = float.MaxValue;
                    int openSetIndex = -1;
                    for (int j = 0; j < openNodes.Length; j++)
                    {
                        int2 nodePos = openNodes[j].position;
                        if(!nodePos.Equals(childPosition)) continue;
                        openSetFCost = openNodes[j].fCost;
                        openSetIndex = j;
                        break;
                    }
                    if(openSetFCost < childFCost) continue;
                    
                    // Check if cheaper version already exists in closed set
                    // bool closedValueExists = closedNodes.TryGetValue(childIndex, out float closedSetFCost);
                    // if(closedValueExists && closedSetFCost < childFCost) continue;

                    // No cheaper version already exists, add new one
                    cameFrom[childIndex] = currentIndex;
                    
                    AStarNode childNode = new AStarNode() {
                        position = childPosition,
                        gCost = childGCost,
                        hCost = childHCost,
                        fCost = childFCost,
                    };

                    if(openSetIndex == -1)
                    {
                        openNodes.Add(childNode);
                    }
                    else
                    {
                        openNodes[openSetIndex] = childNode;
                    }
                }
                
                childPositions.Dispose();
            }
            
            openNodes.Dispose();
            closedNodes.Dispose();
            cameFrom.Dispose();
        }

        private bool IsAtTargetPosition(int2 currentNodePosition)
        {
            // Even if moveNextTo is true, being on target is valid
            if(currentNodePosition.Equals(targetPosition)) return true;
            if(!moveNextToTarget) return false;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int2 position = currentNodePosition + new int2(dx, dy);
                    if(position.Equals(targetPosition)) return true;
                }
            }

            return false;
        }

        private bool IsValidPosition(int2 pos)
        {
            return pos.x >= 0 && pos.x < boardSize.x && pos.y >= 0 && pos.y < boardSize.y;
        }
        
        private int GetIndex(int2 pos)
        {
            return pos.y * boardSize.x + pos.x;
        }

        private int2 IndexToPos(int index)
        {
            int x = index % boardSize.x;
            int y = index / boardSize.x;
            return new int2(x, y);
        }
        
        private static float Heuristic(int2 a, int2 b)
        {
            // Using Euclidean
            return math.distance(a, b);
        }
        
        private void ReconstructPath(NativeHashMap<int, int> cameFrom, int currentIndex)
        {
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            path.Add(IndexToPos(currentIndex));

            while (cameFrom.TryGetValue(currentIndex, out int parentIndex))
            {
                currentIndex = parentIndex;
                path.Add(IndexToPos(currentIndex));
            }

            // Reverse path and copy to result
            for (int i = path.Length - 1; i >= 0; i--)
            {
                resultPath.Add(path[i]);
            }
            
            path.Dispose();
        }
        
        private static NativeArray<int2> GetChildPositions(int2 pos)
        {
            NativeArray<int2> childPositions = new NativeArray<int2>(8, Allocator.Temp);
            int count = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                        
                    childPositions[count] = new int2(pos.x + dx, pos.y + dy);
                    count++;
                }
            }

            return childPositions;
        }
        
        private float GetMoveToParentCost(int2 parentPos, int2 childPos)
        {
            // We assume that they are always next to each other
            if (math.abs(parentPos.x - childPos.x) == 1 && math.abs(parentPos.y - childPos.y) == 1) return 1.4142135623731f; // sqrt(2)
            return 1.0f;
        }
    }
}
