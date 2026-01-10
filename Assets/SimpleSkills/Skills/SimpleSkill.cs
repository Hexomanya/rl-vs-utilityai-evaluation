using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills
{
    [Serializable]
    public abstract class SimpleSkill : ScriptableObject
    {
        public abstract int ID { get; }
        [SerializeField] public string Name;
        [SerializeField] public string Description;
        [SerializeField] public int ActionPointCost;
        [SerializeField] public bool RequiresPosition;
        [SerializeField] public float Range;
        [SerializeField] public Sprite Icon;

        protected string _currentTurnId;
        
        public abstract Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken);
        public abstract Task<bool> ExecuteSkill(SkillContext context, CancellationToken cancelToken);
        
        protected bool IsEnemyTile(SkTileManager tile, ISkAgent originAgent)
        {
            return tile.ContainedEntity is ISkAgent agent && agent.FactionIndex != originAgent.FactionIndex;
        }
    }
}
