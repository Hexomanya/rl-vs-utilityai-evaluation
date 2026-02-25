using System.Collections.Generic;
using _General;
using ArtificialEnemy;
using UnityEngine;

namespace SimpleSkills
{
    public interface ISkAgent : IAgent, ITileContainable
    {
        public SkGameplayManager GameplayManager { get; }
        public Vector2Int Position { get; set; }
        public int ActionPoints { get; set; }
        public int FactionIndex { get; }
        public bool EndTurnAfter { get; set; }
        public Attribute<int> Health { get; }
        public IReadOnlyList<SimpleSkill> Skills { get; }
        public float MovementRange { get; }

        public void CancelTasks();
        
        public void DealDamage(ISkAgent targetAgent, int damage);
        public bool TakeDamage(ISkAgent mlSkAgent, int damage);
        public void Reset();

        bool ITileContainable.IsObstruction { get => true; }
    }
}
