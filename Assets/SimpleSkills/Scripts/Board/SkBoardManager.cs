using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _General;
using _General.Custom_Attributes;
using _General.TypeExtensions;
using KBCore.Refs;
using SimpleSkills.Configs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SimpleSkills
{
    [RequireComponent(typeof(BoardPathfindingSystem))]
    public class SkBoardManager : ValidatedMonoBehaviour
    {
        [SerializeField, Required] private GameObject _tilePrefab;
        [SerializeField, Self] private BoardPathfindingSystem _pathfindingSystem;
        [SerializeField] private int _pathfindingPollingIntervalMs = 20;
        [SerializeField] private int _pathfindingTimeoutTime = 200;

        public string OwnerID = "";

        private readonly List<SkTileManager> _tiles = new List<SkTileManager>();
        private TestMap _testMap;
        
        public IReadOnlyList<SkTileManager> Tiles { get => _tiles; }
        
        public int Width { get => _testMap?.Width ?? BoardConfig.Width; }
        public int Height { get => _testMap?.Height ?? BoardConfig.Height; }
        
        private int TileCount { get => this.Height * this.Width; }
        
        public void Reset()
        {
            foreach (SkTileManager tileValueManager in _tiles)
            {
                tileValueManager.Clear();
            }
        }

        public void Initialize(int testMapIndex, string owningID)
        {
            this.OwnerID = owningID;
            if(_tiles.Count > 0)
            {
                Debug.LogWarning("The Board was already initialized! Use Reset instead!");
                this.Reset();
                return;
            }

            Debug.Log($"Initializing Game Board from owner {OwnerID}");
            
            this.InitializeTestMap(testMapIndex);
            
            const float tileScale = BoardConfig.CellSize - BoardConfig.CellBorder;

            for (int i = 0; i < this.TileCount; i++)
            {
                Vector2Int columnRow = OneDimUtil.GetPosition(i, this.Width, this.Height);
                float xPos = columnRow.x * BoardConfig.CellSize;
                float yPos = columnRow.y * BoardConfig.CellSize;

                GameObject tileObject = Object.Instantiate(_tilePrefab, this.transform);
                tileObject.transform.localScale = new Vector3(tileScale, tileScale, 1);
                tileObject.transform.position = new Vector3(xPos, 0, yPos);

                if(!tileObject.TryGetComponent(out SkTileManager tileManager))
                {
                    Debug.LogError("The provided Tile-Prefab has no TileManager-Component on it!");
                    return;
                }

                tileManager.DebugBoardPosition = columnRow;
                tileManager.UiController.SetPositionText($"[{columnRow.x},{columnRow.y}]");
                _tiles.Add(tileManager);
            }
            
            this.NewGame();
            Debug.Log($"Game Board Initialized with {_tiles.Count} tiles.");
        }

        private void InitializeTestMap(int testMapIndex)
        {
            List<TestMap> testMaps = BoardConfig.GetTestMaps();
            bool isInBounds = testMapIndex >= 0 && testMapIndex < testMaps.Count;
            
            if(isInBounds)
            {
                _testMap = testMaps[testMapIndex];
            }
            else
            {
                _testMap = new TestMap() {
                    Width = BoardConfig.Width,
                    Height = BoardConfig.Height,
                    IsMapValid = false,
                    Map = new List<int>(),
                };
            }
        }

        private SkTileManager GetTileAt(int x, int y)
        {
            if(!this.IsInBounds(x,y))
            {
                //Debug.LogWarning($"Grid position ({x}, {y}) is out of bounds!");
                return null;
            }

            int index = OneDimUtil.GetIndex(x,y,this.Width);

            if(index >= _tiles.Count)
            {
                Debug.LogError($"Calculated index {index} is out of range for tiles array (count: {_tiles.Count})! Owner is: {this.OwnerID}");
                return null;
            }

            SkTileManager tile = _tiles[index];

            if(tile is not null)
            {
                return tile;
            }

            Debug.LogError($"Found tile at ({x},{y}) is null!");
            return null;
        }

        public SkTileManager GetTileAt(Vector2Int gridPos)
        {
            return this.GetTileAt(gridPos.x, gridPos.y);
        }

        public bool TryGetFreePosAround(Vector2Int startPos, out Vector2Int safePos, int maxRadius = 3)
        {
            safePos = Vector2Int.zero;


            SkTileManager startTile = this.GetTileAt(startPos);

            if(this.IsTileFree(startTile))
            {
                safePos = startPos;
                return true;
            }

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int x = startPos.x - radius; x <= startPos.x + radius; x++)
                {
                    for (int y = startPos.y - radius; y <= startPos.y + radius; y++)
                    {
                        if(Mathf.Abs(x - startPos.x) != radius && Mathf.Abs(y - startPos.y) != radius)
                        {
                            continue;
                        }

                        Vector2Int checkPos = new Vector2Int(x, y);
                        SkTileManager tile = this.GetTileAt(checkPos);

                        if(this.IsTileFree(tile))
                        {
                            safePos = checkPos;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public List<SkTileManager> GetTilesInRadiusWithQuery(Vector2Int center, int radius, Func<SkTileManager, bool> query)
        {
            List<SkTileManager> tiles = this.GetTilesInRadius(center, radius);
            return tiles.Where(query).ToList();
        }

        public bool MoveAgent(ISkAgent agent, Vector2Int newPosition, Vector2Int oldPosition)
        {
            SkTileManager newTile = this.GetTileAt(newPosition);
            SkTileManager oldTile = this.GetTileAt(oldPosition);
            
            if(!newTile.IsFree())
            {
                Debug.LogWarning(
                    $"Can not move agent from {oldPosition}, because new position at {newPosition} is already occupied! This should have been checked sooner!");
                return false;
            }
            
            oldTile.ContainedEntity = null;
            newTile.ContainedEntity = agent;
            
            List<int> tileIndices = new List<int> { OneDimUtil.GetIndex(newPosition, this.Width), OneDimUtil.GetIndex(oldPosition, this.Width) };
            List<SkTileManager> tiles = new List<SkTileManager> {newTile, oldTile};
            _pathfindingSystem.WalkabilityMap.UpdateAtIndices(tileIndices, tiles);
            return true;
        }

        //TODO: Different radius types
        private List<SkTileManager> GetTilesInRadius(Vector2Int center, int radius, bool includeCenter = false)
        {
            List<SkTileManager> tiles = new List<SkTileManager>();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if(!includeCenter && dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    Vector2Int checkPos = center + new Vector2Int(dx, dy);

                    SkTileManager tile = this.GetTileAt(checkPos.x, checkPos.y);

                    if(tile is null) continue;
                    
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        private bool IsTileFree(SkTileManager tileManager)
        {
            return tileManager is not null && tileManager.IsFree();
        }

        public void ClearTile(Vector2Int position)
        {
            SkTileManager tile = this.GetTileAt(position);
            tile.Clear();
            
            List<int> tileIndices = new List<int> { OneDimUtil.GetIndex(position, this.Width)};
            List<SkTileManager> tiles = new List<SkTileManager> {tile};
            _pathfindingSystem.WalkabilityMap.UpdateAtIndices(tileIndices, tiles);
        }
        
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        public float[,,] ComposeActorGrids(Vector2Int center, int factionIndex)
        {
            const int channelCount = ObservationConfig.ChannelCount;
            const int gridSize = ObservationConfig.GridSize;
            const int radius = (gridSize - 1) / 2;
            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse => For When we change the const
            if(gridSize % 2 != 1)
            {
                Debug.LogError("GridSize must be odd for correct centering");
            }
            
            float[,,] actorGrids = new float[channelCount, gridSize, gridSize];
            for (int c = 0; c < channelCount; c++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        actorGrids[c, y, x] = -1f;
                    }
                }
            }
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int currentPos = center + new Vector2Int(dx, dy);
                    SkTileManager tile = this.GetTileAt(currentPos);

                    if (tile is null)
                    {
                        //OUTSIDE OF MAP, LEAVE AT -1
                        continue;
                    }
                    
                    int xIndex = dx + radius;
                    int yIndex = dy + radius;

                    if (tile.ContainedEntity is MlSkAgent agent)
                    {
                        bool isFriendly = agent.FactionIndex == factionIndex;
                        actorGrids[ObservationConfig.FriendlyChannelIndex, yIndex, xIndex] = isFriendly ? 1f : 0f;
                        actorGrids[ObservationConfig.EnemyChannelIndex, yIndex, xIndex] = !isFriendly ? 1f : 0f; 
                    }
                    
                    actorGrids[ObservationConfig.ObstacleChannelIndex, yIndex, xIndex] = tile.ContainedEntity is Wall ? 1f : 0f;

                    // if(tile.ContainedEntity is Resource resource)
                    // {
                    //     actorGrids[ObservationConfig.ResourceChannelIndex, yIndex, xIndex] = resource.ContainedElement.MapIndex;
                    // }
                }
            }

            return actorGrids;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < this.Width && y < this.Height;
        }
        
        public bool IsInBounds(Vector2Int position)
        {
            return this.IsInBounds(position.x, position.y);
        }
        
        public bool IsInBounds(int2 position)
        {
            return this.IsInBounds(position.x, position.y);
        }

        public void NewGame()
        {
            this.Clear();

            if(!_testMap.IsMapValid)
            {
                this.PlaceWalls();
                this.PlaceResources();
            }
            else
            {
                this.CreateTestMap();
            }
            
            _pathfindingSystem.WalkabilityMap.Refresh(_tiles);
        }
        
        private void Clear()
        {
            foreach (SkTileManager tile in _tiles)
            {
                tile.Clear();
            }
            
            _pathfindingSystem.Clear();
        }
        
        private void PlaceWalls()
        {
            int wallCount = Mathf.FloorToInt(this.TileCount * BoardConfig.WallPercentage);

            for (int i = 0; i < wallCount; i++)
            {
                Vector2Int randomPos = new Vector2Int(Random.Range(0, this.Width), Random.Range(0, this.Height));
                
                if(! this.TryGetFreePosAround(randomPos, out Vector2Int safePos)) continue;
                this.GetTileAt(safePos).ContainedEntity = new Wall();
            }
        }

        private void PlaceResources()
        {
            for (int i = 0; i < BoardConfig.ResourceCount; i++)
            {
                List<Resource> resources = new List<Resource> {
                    new Resource(new FireElement(), 3),
                    new Resource(new WaterElement(), 3),
                    new Resource(new EarthElement(), 3),
                    new Resource(new AirElement(), 3),
                };


                foreach (Resource resource in resources)
                {
                    Vector2Int randomPos = new Vector2Int(Random.Range(0, this.Width), Random.Range(0, this.Height));
                
                    if(! this.TryGetFreePosAround(randomPos, out Vector2Int safePos)) continue;
                    this.GetTileAt(safePos).ContainedEntity = resource;
                }
            }
        }

        private void CreateTestMap()
        {
            if(!_testMap.IsMapValid)
            {
                Debug.LogError($"TestMap is not valid!)");
                return;
            }

            for (int i = 0; i < _testMap.Map.Count; i++)
            {
                int tileContentIndicator = _testMap.Map[i];
                if(tileContentIndicator <= -1) continue;

                SkTileManager tile = this.GetTileAt(OneDimUtil.GetPosition(i, this.Width, this.Height));

                if(tile is null)
                {
                    Debug.LogWarning($"Could not retrieve tile at index: {i}.");
                    continue;
                }
                
                ITileContainable tileContent = tileContentIndicator switch
                {
                    0 => new Wall(),
                    10 => new Resource(new WaterElement(), 3),
                    11 => new Resource(new FireElement(), 3),
                    12 => new Resource(new EarthElement(), 3),
                    13 => new Resource(new AirElement(), 3),
                    _ => null,
                };

                if(tile.ContainedEntity is not null)
                {
                    Debug.LogWarning($"Could not fill tile. It was already occupied!");
                    continue;
                }

                tile.ContainedEntity = tileContent;
            }
        }

        public float TileToWorldDistance(Vector2Int startPos, Vector2Int endPos)
        {
            return Vector2Int.Distance(startPos, endPos) * BoardConfig.CellSize;
        }
        
        //Somehow wrong when sorting
        public float TileToWorldDistanceSquare(Vector2Int startPos, Vector2Int endPos)
        {
            return startPos.SquaredDistance(endPos) * BoardConfig.CellSize;
        }
        
        //Bresenham line-drawing algorithm
        public bool HasLineOfSight(Vector2Int originPos, Vector2Int targetPos)
        {
            int x0 = originPos.x;
            int y0 = originPos.y;
            int x1 = targetPos.x;
            int y1 = targetPos.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int stepX = x0 < x1 ? 1 : -1;
            int stepY = y0 < y1 ? 1 : -1;
            int error = dx - dy;

            while (true)
            {
                Vector2Int currentPosition = new Vector2Int(x0, y0);

                // Check this tile for obstruction
                if(currentPosition != originPos && currentPosition != targetPos)
                {
                    SkTileManager tile = this.GetTileAt(currentPosition);
                    if(tile is null) return false;
                    if (tile.ContainsObstruction()) return false;
                }
                
                // If we've reached the target cell, we have line of sight
                if (currentPosition == targetPos) return true;

                int error2 = error * 2;
                if (error2 > -dy)
                {
                    error -= dy;
                    x0 += stepX;
                }
                if (error2 < dx)
                {
                    error += dx;
                    y0 += stepY;
                }
            }
        }
        public Resource GetResourceInRadius(Vector2Int originPosition, Type requiredElementType, int radius)
        {
            List<Resource> resources = this.GetTilesInRadius(originPosition, radius).Where(tile => tile.ContainsResourceOfType(requiredElementType))
                .Select(tile => tile.ContainedEntity as Resource).ToList();

            if(resources.Count == 0) return null;
            return resources.Random();
        }
        
        public ISkAgent GetAgentOnTileOrInRange(Vector2Int targetPos, Vector2Int originPos, float range)
        {
            SkTileManager targetTile = this.GetTileAt(targetPos);
            if(targetTile is not null)
            {
                if(targetTile.TryGetAgent(out ISkAgent targetAgent)) return targetAgent;
            }

            List<SkTileManager> occupiedTiles = this.GetTilesInRadiusWithQuery(originPos, (int)range, tile => tile.ContainedEntity is MlSkAgent agent && agent.Position != originPos);
            return occupiedTiles
                .Select(tile => tile.ContainedEntity as MlSkAgent)
                .OrderBy(agent =>
                {
                    if(agent is null) return float.MaxValue;
                    return Vector2Int.Distance(originPos, agent.Position);
                }).FirstOrDefault();
        }

        private Vector3 GetOrigin()
        {
            const float halfCell = BoardConfig.CellSize / 2;
            return transform.position - new Vector3(halfCell, 0, halfCell);
        }
        
        public Vector2Int WorldToBoardPosition(Vector3 worldPosition)
        {
            Vector3 relativePosition = worldPosition - this.GetOrigin();
            
            return new Vector2Int(
                Mathf.FloorToInt(relativePosition.x / BoardConfig.CellSize),
                Mathf.FloorToInt(relativePosition.z / BoardConfig.CellSize)
            );
        }
        
        public async Task GetPath(PathfindingRequest request, CancellationToken cancelToken)
        {
            //Debug.Log("GetPath");
            _pathfindingSystem.QueuePathfindingRequest(request);
            
            while (!request.IsDone && request.ElapsedTimeMs < _pathfindingTimeoutTime && !cancelToken.IsCancellationRequested)
            {
                await Task.Delay(_pathfindingPollingIntervalMs, cancelToken);
                request.AddElapsedTime(_pathfindingPollingIntervalMs);
            }
            
            if(request.ElapsedTimeMs >= _pathfindingTimeoutTime) Debug.LogWarning("Pathfinding timed out!");
            if(!request.IsDone) Debug.LogWarning("Pathfinding timed out!");
        }
        
        public Vector2Int GetMaximumReachableTargetPosition (ISkAgent movedAgent, PathResult pathResult)
        {
            float movementDistance = movedAgent.MovementRange;
            
            if(pathResult.ResultPath.Count < 2)
            {
                Debug.LogWarning("Path was smaller then two, agent is already at position!");
// #if UNITY_EDITOR
//                 UnityEditor.EditorApplication.isPaused = true;
// #endif
                return movedAgent.Position;
            }

            //Debug.Log($"Path length is {path.PathLength} without last it is {path.PathLengthWithoutLast}.");
            // Agent can reach destination within one Action
            if(movementDistance >= pathResult.PathLength)
            {
                Vector2Int newPosition = pathResult.ResultPath.Last();
                SkTileManager tile = this.GetTileAt(newPosition);

                if(this.IsTileFree(tile)) return newPosition;
                
                bool isMovedAgent = tile.ContainedEntity is ISkAgent agent && agent.ID == movedAgent.ID;
                if(isMovedAgent)
                {
                    Debug.LogWarning($"Position was the position the agents was already standing on.");
                    return movedAgent.Position;
                }
            }

            //Debug.Log($"Start from: {movedAgent.Position}; {pathResult.ResultPath[0]}");
            
            float remainingMovement = movementDistance;
            Vector2Int reachedPosition = movedAgent.Position;
            //Agent can't reach destination, search tile he can reach.
            for (int i = 0; i < pathResult.ResultPath.Count; i++)
            {
                if(i == pathResult.ResultPath.Count - 1)
                {
                    //Reached end
                    Debug.LogWarning("Reached end of path, but used stepping doing so. This should not happen!");
                    reachedPosition = pathResult.ResultPath.Last();
                    break;
                }

                // Debug.Log($"Next tile is at: {pathResult.ResultPath[i+1]}");
                bool isNextTileFree = this.IsTileFree(this.GetTileAt(pathResult.ResultPath[i + 1]));
                float distanceToNext = this.TileToWorldDistance(pathResult.ResultPath[i], pathResult.ResultPath[i + 1]);

                if(!isNextTileFree || remainingMovement < distanceToNext)
                {
                    //Debug.Log($"Quitting out at: {pathResult.ResultPath[i]}, isNextTileFree: {isNextTileFree}, distanceToNext: {distanceToNext}/{pathResult.PathLength}, {remainingMovement}/{movedAgent.MovementRange}(Remaining)");
                    reachedPosition = pathResult.ResultPath[i];
                    break;
                }

                //Debug.Log($"Step from: {pathResult.ResultPath[i]}");
                remainingMovement -= distanceToNext;
            }
            
            SkTileManager reachedTile = this.GetTileAt(reachedPosition);

            if(this.IsTileFree(reachedTile))
            {
                //Debug.Log($"Reached position is: {reachedPosition}");
                return reachedPosition;
            }
            
            //Debug.Log("Chosen tile was not safe!");
            //string pathstring = string.Join(", ", pathResult.ResultPath.Select(p => $"({p.x},{p.y})"));
            //Debug.Log($"Path was: {pathstring}");
            
            return movedAgent.Position;
        }
        public ValueMap ComputeSafetyMap(ISkAgent originAgent)
        {
            Bounds2Int mapBounds = new Bounds2Int(0, this.Width, this.Height);
            Bounds2Int boardBounds = new Bounds2Int(0, this.Width, this.Height);
            ValueMap safety = new ValueMap(mapBounds, boardBounds);
            
            for (int dx = 0; dx < this.Width; dx++)
            {
                for (int dy = 0; dy < this.Height; dy++)
                {
                    Vector2Int currentPosition = new Vector2Int(dx, dy);

                    bool isInBounds = this.IsInBounds(currentPosition);
                    bool isInRange = this.TileToWorldDistance(originAgent.Position, currentPosition) <= originAgent.MovementRange;
                    
                    if(!isInBounds || !isInRange)
                    {
                        safety.SetValue(currentPosition, null);
                        continue;
                    }
                    
                    int safetyValue = this.ComputeTileSafety(originAgent, currentPosition);
                    safety.SetValue(currentPosition, safetyValue);
                }
            }

            return safety;
        }

        public int WorldToTileDistance(float worldDistance, bool roundDown = false)
        {
            float tileDistance = Mathf.Abs(worldDistance) / BoardConfig.CellSize;
            return roundDown ? Mathf.FloorToInt(tileDistance) : Mathf.CeilToInt(tileDistance);
        }
        
        //TODO: Make more complex with cover and used skills. And also break into functions
        private int ComputeTileSafety(ISkAgent originAgent, Vector2Int position)
        {
            const float nearEnemySafety = -1.2f;
            const float nearFriendlySafety = 1f;
            const float nearWallSafety = 0.25f;
            
            float safety = 0;
            int friendlyIndex = originAgent.FactionIndex;
            List<SkTileManager> surroundingTiles = this.GetTilesInRadius(position, 1, true);


            foreach (SkTileManager tile in surroundingTiles)
            {
                switch (tile.ContainedEntity)
                {
                    case null:
                        continue;

                    case ISkAgent agent when agent.FactionIndex != friendlyIndex:
                        safety += nearEnemySafety; // Enemies
                        break;

                    case ISkAgent agent when agent.ID != originAgent.ID:
                        safety += nearFriendlySafety; // Friendlies, but also neutral
                        break;

                    case Wall:
                        safety += nearWallSafety; // Walls
                        break;
                }
            }
            
            return Mathf.RoundToInt(safety * ValueMap.VALUE_MULTIPLIER);
        }
        
        public async Task<Vector2Int?> GetClosestReachableSafePosition(ValueMap safetyMap, ISkAgent originAgent, CancellationToken cancelToken)
        {
            if(safetyMap.Values.Length <= 0)
            {
                Debug.LogWarning("Provided Danger map is empty!");
                return null; 
            }
            
            //Dictionary<safetyValue, List<positionIndex>>
            SortedDictionary<int, List<int>> sortedSafetyMap = new SortedDictionary<int, List<int>>(
                Comparer<int>.Create((a, b) => b.CompareTo(a))
            );

            for (int i = 0; i < safetyMap.Values.Length; i++)
            {
                if(safetyMap.Values[i] is null) continue;
                int safetyValue = safetyMap.Values[i].Value;
                
                List<int> positionIndices = sortedSafetyMap.GetValueOrDefault(safetyValue, new List<int>());
                positionIndices.Add(i);
                
                sortedSafetyMap[safetyValue] = positionIndices;
            }

            
            // Go from safest => unsafest and try to find a path to the closest position
            foreach (KeyValuePair<int, List<int>> positionIndices in sortedSafetyMap)
            {
                List<Vector2Int> sortedSafePosition = positionIndices.Value
                    .Select(safetyMap.IndexToBoardPosition)
                    .OrderBy(boardPosition => this.TileToWorldDistance(originAgent.Position, boardPosition)).ToList();
                    //.OrderByDescending(boardPosition => this.TileToWorldDistance(originAgent.Position, boardPosition)).ToList();
                
                foreach (Vector2Int safePosition in sortedSafePosition)
                {
                    if(cancelToken.IsCancellationRequested) return null;

                    if(safePosition == originAgent.Position) return safePosition;
                    if(!this.IsTileFree(this.GetTileAt(safePosition))) continue;
                    
                    //if(this.TileToWorldDistance(safePosition, originAgent.Position) > originAgent.MovementRange) continue;
                    
                    PathfindingRequest request = new PathfindingRequest(originAgent.Position, safePosition, false, this);
                    await this.GetPath(request, cancelToken); //TODO: Do in parallel if performance is low, Currently not a problem on 1 vs 1 because if it is in range it can almost certainly be reached
                    
                    if(!request.IsDone) Debug.LogWarning("Request was not done!");
                    if(!request.Result.DidFindPath || request.Result.PathLength > originAgent.MovementRange) continue;
                    
                    // Position can be reached, return it.
                    return safePosition;
                }
            }
            
            Debug.LogWarning("Could not find any position that was reachable!");
            return null;
        }
    }
}
