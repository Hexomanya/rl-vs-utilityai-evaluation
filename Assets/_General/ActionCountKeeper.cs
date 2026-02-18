using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace _General
{
    public static class ActionCountKeeper
    {
        private static Dictionary<string, Dictionary<string, int>> _actionUses = new Dictionary<string, Dictionary<string, int>>();
        
        public static void CountActionUse(string agentType, string actionType)
        {
            if (!_actionUses.TryGetValue(agentType, out Dictionary<string, int> recordCategory))
            {
                recordCategory = new Dictionary<string, int>();
                _actionUses[agentType] = recordCategory;
            }
    
            recordCategory.TryGetValue(actionType, out int value);
            recordCategory[actionType] = value + 1;
        }
        
        public static void Print()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Final action uses:");

            foreach (KeyValuePair<string, Dictionary<string, int>> agentActionDictionary in _actionUses)
            {
                foreach (KeyValuePair<string, int> actionUse in agentActionDictionary.Value)
                {
                    stringBuilder.AppendLine($"Agent {agentActionDictionary.Key} has used the action {actionUse.Key} {actionUse.Value} times.");
                }
                stringBuilder.AppendLine("---");
            }
            
            Debug.Log(stringBuilder.ToString());
        }
    }
}
