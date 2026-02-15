using System;
using System.Collections.Generic;
using Unity.MLAgents;

namespace SimpleSkills
{
    public class SkillUseCounter
    {
        private readonly Dictionary<string, Dictionary<string, int>> _skillUses = new Dictionary<string, Dictionary<string, int>>();
        
        public void CountSkillUse(string category, string skillName)
        {
            if (!_skillUses.TryGetValue(category, out Dictionary<string, int> recordCategory))
            {
                recordCategory = new Dictionary<string, int>();
                _skillUses[category] = recordCategory;
            }
    
            recordCategory.TryGetValue(skillName, out int value);
            recordCategory[skillName] = value + 1;
        }

        public void ReportAndClear()
        {
            StatsRecorder statsRecorder = Academy.Instance.StatsRecorder;
            
            foreach (KeyValuePair<string, Dictionary<string, int>> recordCategory in _skillUses)
            {
                foreach (KeyValuePair<string, int> skillUse in recordCategory.Value)
                {
                    statsRecorder.Add(recordCategory.Key + "/" + skillUse.Key, skillUse.Value, StatAggregationMethod.Sum);
                }
            }
            
            _skillUses.Clear();
        }
    }
}
