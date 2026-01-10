using System.Collections.Generic;
using System.Text;
using _General;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleSkills
{
    public class WalkabilityMap
    {
        private NativeArray<bool> _walkabilityMap;
        private bool _walkabilityMapInitialized = false;
        private bool _isDirty = false;
        
        public NativeArray<bool> Value { get => _walkabilityMap; }

        public void Refresh(IReadOnlyList<SkTileManager> tiles)
        {
            if (!_walkabilityMapInitialized)
            {
                _walkabilityMap = new NativeArray<bool>(tiles.Count, Allocator.Persistent);
                _walkabilityMapInitialized = true;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                SkTileManager tile = tiles[i];
                _walkabilityMap[i] = tile.IsWalkable();
            }
        }

        public void UpdateAtIndices(List<int> tileIndices, List<SkTileManager> tiles)
        {
            if (tileIndices.Count != tiles.Count)
            {
                Debug.LogError($"Index/tile count mismatch: {tileIndices.Count} indices vs {tiles.Count} tiles. Update aborted.");
                return;
            }
            
            for (int i = 0; i < tileIndices.Count; i++)
            {
                int index = tileIndices[i];
                bool isInBounds = index >= 0 && index < _walkabilityMap.Length;
                
                if (!isInBounds)
                {
                    Debug.LogWarning($"Tile index {index} out of bounds (0, {_walkabilityMap.Length}). Skipping.");
                    continue;
                }
                
                _walkabilityMap[index] = tiles[i].IsWalkable();
            }
        }
        
        public void ReadyUse(IReadOnlyList<SkTileManager> tiles)
        {
            if(_walkabilityMapInitialized && !_isDirty) return;
            this.Refresh(tiles);
        }

        public void Dispose()
        {
            if(_walkabilityMap.IsCreated) _walkabilityMap.Dispose();
        }
        
        public void PrintWalkabilityMap(int width, int height, int2? start = null, int2? target = null)
        {
            if (!_walkabilityMapInitialized || !_walkabilityMap.IsCreated)
            {
                Debug.LogWarning("Walkability map not initialized!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Walkability Map ({width}x{height}):");
            sb.AppendLine("Legend: . = walkable, # = blocked, S = start, T = target, X = start/target overlap");
            sb.AppendLine();

            // Print column numbers header
            sb.Append("   ");
            for (int x = 0; x < width; x++)
            {
                sb.Append($"{x % 10}");
            }
            sb.AppendLine();

            for (int y = height - 1; y >= 0; y--) // Print from top to bottom
            {
                // Print row number
                sb.Append($"{y,2} ");

                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int2 currentPos = new int2(x, y);
                    
                    bool isStart = start.HasValue && currentPos.Equals(start.Value);
                    bool isTarget = target.HasValue && currentPos.Equals(target.Value);

                    if (isStart && isTarget)
                    {
                        sb.Append("X");
                    }
                    else if (isStart)
                    {
                        sb.Append("S");
                    }
                    else if (isTarget)
                    {
                        sb.Append("T");
                    }
                    else if (index < _walkabilityMap.Length && _walkabilityMap[index])
                    {
                        sb.Append(".");
                    }
                    else
                    {
                        sb.Append("#");
                    }
                }
                
                sb.Append($" {y}"); // Print row number on right side too
                sb.AppendLine();
            }

            // Print column numbers footer
            sb.Append("   ");
            for (int x = 0; x < width; x++)
            {
                sb.Append($"{x % 10}");
            }
            sb.AppendLine();

            Debug.Log(sb.ToString());
        }

    }
}
