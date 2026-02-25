using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace _General
{
    public struct ExecutionRecord
    {
        public float OverallTime;
        public int ExecutionCount;
    }
    
    public static class ExecutionTimeKeeper
    {
        private static readonly Dictionary<string, Dictionary<string, ExecutionRecord>> _actionTime = new Dictionary<string, Dictionary<string, ExecutionRecord>>();
        
        public static void AddActionTime(string agentType, string actionType, float timeMilliseconds)
        {
            if (!_actionTime.TryGetValue(agentType, out Dictionary<string, ExecutionRecord> recordCategory))
            {
                recordCategory = new Dictionary<string, ExecutionRecord>();
                _actionTime[agentType] = recordCategory;
            }
    
            recordCategory.TryGetValue(actionType, out ExecutionRecord value);
            
            value.ExecutionCount++;
            value.OverallTime += timeMilliseconds;
            recordCategory[actionType] = value;
        }
        
        public static void Print()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Final execution time:");
            foreach (KeyValuePair<string, Dictionary<string, ExecutionRecord>> agentActionDictionary in _actionTime)
            {
                foreach ((string key, ExecutionRecord record) in agentActionDictionary.Value)
                {
                    stringBuilder.AppendLine(
                        $"Agent {agentActionDictionary.Key} has used the action {key} " +
                        $"{record.ExecutionCount} times. " +
                        $"Overall: {record.OverallTime}ms, " +
                        $"Average: {(record.ExecutionCount > 0 ? record.OverallTime / record.ExecutionCount : 0)}ms"
                    );
                }
                stringBuilder.AppendLine("---");
            }
            
            Debug.Log(stringBuilder.ToString());
        }
    }
}
