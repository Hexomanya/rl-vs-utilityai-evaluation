using System.Collections.Generic;
using UnityEngine;

namespace SimpleSkills
{
    public class SkillContext
    {
        public readonly ISkAgent OriginAgent;
        public Vector2Int? TargetPosition;
        public IReadOnlyList<ISkAgent> TargetAgents;
        
        public readonly string TurnId;
        public bool IsValidated = false;
        
        public SkillContext(ISkAgent originAgent, string turnId, Vector2Int? targetPosition = null, List<ISkAgent> targetAgents = null)
        {
            this.OriginAgent = originAgent;
            this.TurnId = turnId;
            
            this.TargetPosition = targetPosition;
            this.TargetAgents = targetAgents;
        }
    }
}
