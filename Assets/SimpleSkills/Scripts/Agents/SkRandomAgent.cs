using System;
using System.Collections.Generic;
using System.Threading;
using _General;
using SimpleSkills.Scripts;
using SimpleSkills.Scripts.RewardProvider;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SimpleSkills
{
    public class SkRandomAgent : MonoBehaviour, ISkAgent
    {
        [Header("Configuration")]
        [SerializeField] private int _maxHealth = 10;
        [SerializeField] private List<SimpleSkill> _skills = new List<SimpleSkill>();

        [Header("Events")]
        [SerializeField] private SkAgentEvent _currentAgentChangedEvent;
        [SerializeField] private SkillEvent _lastUsedSkillChangedEvent;

        // Core Identity
        private string _id;
        private string _agentName;
        private int _agentIndex;
        private int _factionIndex;

        // Gameplay State
        private SkGameplayManager _manager;
        private Vector2Int _position;
        private int _actionPoints;
        private bool _endTurnAfter;
        
        // Async
        private CancellationTokenSource _cancelTokenSource;

        // Properties
        public string ID { get => _id; }
        public string CurrentTurnId { get; private set; }
        public int FactionIndex { get => _factionIndex; }
        public Vector2Int Position { get => _position; set => this.SetPosition(value); }
        public SkGameplayManager GameplayManager { get => _manager; }
        public int ActionPoints 
        { 
            get => _actionPoints; 
            set 
            {
                _actionPoints = value;
                this.OnDisplayValueChange(); 
            } 
        }
        public Attribute<int> Health { get; private set; }

        public bool EndTurnAfter { get => _endTurnAfter; set => _endTurnAfter = value; }
        public IReadOnlyList<SimpleSkill> Skills { get => _skills; }
        public float MovementRange { get => 4.5f; }

        // Interface Implementations
        public Color TileColor { get => this.GetTileColor(); }
        
        private void OnDestroy()
        {
            this.CancelTasks();
        }

        public void CancelTasks()
        {
            if(_cancelTokenSource == null) return;
            try {
                if (!_cancelTokenSource.IsCancellationRequested) _cancelTokenSource.Cancel();
            }
            catch (ObjectDisposedException) { }
            finally
            {
                _cancelTokenSource.Dispose();
                _cancelTokenSource = null;
            }
        }
        
        
        public void SetupAgent(string agentName, int factionIndex, IGameplayManager manager)
        {
            this.CancelTasks();
            
            _id = Guid.NewGuid().ToString();
            _agentName = agentName;
            _factionIndex = factionIndex;
            this.Health = new Attribute<int>(_maxHealth, _maxHealth);
            
            _manager = manager as SkGameplayManager;
            _cancelTokenSource = new CancellationTokenSource();

            
            this.Reset();
        }
        
        public string GetName()
        {
            return _agentName;
        }
        public void OnTurnStart()
        {
            //Debug.Log($"Start of {_id}s turn! ---------------------------");

            this.CurrentTurnId = Guid.NewGuid().ToString();
            this.ActionPoints = Mathf.Min(this.ActionPoints + GameConfig.ActionPointTurnRecover, GameConfig.ActionPointMax);
            _endTurnAfter = false;
            
            this.ChooseSkill();
        }
        
        public void EndTurn()
        {
            // Debug.Log($"End of {_id}s turn! ------------------------");
            _manager.OnTurnDone(this);
        }
        
        public void OnGameEnd(WinLooseSignal result) { }

        private void Die(ISkAgent killOriginAgent)
        {
            _manager.RemoveFromGame(this);
            this.GameplayManager.ScoreKeeper.AddScore(ScoreType.Death, this.FactionIndex);
            if(_manager.RunConfig.IsTestRun) Debug.Log($"{this.GetName()} was killed by {killOriginAgent.GetName()}");
            GameLog.Print($"Was killed by {killOriginAgent.GetName()}.", this);
        }

        public bool TakeDamage(ISkAgent damageOrigin, int damage)
        {
            this.Health.Set( Mathf.Max(0, this.Health.Value - damage));
            //Debug.Log($"{this.GetName()} received {damage} damage from {damageOrigin.GetName()}.");

            bool willDie = this.Health.Value <= 0;
            if(willDie) this.Die(damageOrigin);

            return willDie;
        }
        
        public void DealDamage(ISkAgent damageTarget, int damage)
        {
            bool killedTarget = damageTarget.TakeDamage(this, damage);

            if(killedTarget) this.GameplayManager.ScoreKeeper.AddScore(ScoreType.Kill, this.FactionIndex);;
        }
        
        public void ChooseSkill()
        {
            SimpleSkill skill = _skills[Random.Range(0, _skills.Count)];
            int width = this.GameplayManager.BoardManager.Width;
            int height = this.GameplayManager.BoardManager.Height;

            Vector2Int position = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            
            this.ExecuteAction(skill, position);
        }
        
        private async void ExecuteAction(SimpleSkill skill, Vector2Int boardPosition)
        {
            SkillContext context = new SkillContext(this, this.CurrentTurnId, boardPosition);
            //Debug.Log($"Executing skill {skill.Name}");
            
            if(skill.ActionPointCost > this.ActionPoints)
            {
                //Debug.Log($"Agent selected skill with a too high action point cost: {skill.ActionPointCost} > {_actionsPoints}.");
                this.RetryGetAction();
                return;
            }

            bool canExecute = await skill.CanExecute(context, _cancelTokenSource.Token);

            if(!canExecute)
            {
                //Debug.LogWarning($"The skill {skill.Name} should have been masked!");
                this.RetryGetAction();
                return;
            }
            
            bool didExecute = await skill.ExecuteSkill(context, _cancelTokenSource.Token);
            
            if(!didExecute)
            {
                Debug.LogWarning($"The skill {skill.Name} did not execute and should have been masked!");
                this.RetryGetAction();
                return;
            }
            
            if(this.GameplayManager.RunConfig.IsTestRun)
            {
                Debug.Log($"Agent {this.GetName()} successfully used skill {skill.Name}.");
            }
            
            GameLog.Print($"Used skill: {skill.Name}.", this);
            _lastUsedSkillChangedEvent.Raise(skill);
            
            this.ActionPoints -= skill.ActionPointCost;
            bool didEndGame = _manager.OnActionTook(this, -1);

            if(this.ActionPoints == 0 || _endTurnAfter || didEndGame)
            {
                this.EndTurn();
                return;
            }
            
            this.ChooseSkill();
        }

        private void RetryGetAction()
        {
            //Debug.Log($"Agent {_id} reached their max allowed tries. Ending its turn.");
            this.EndTurn();
        }

        private void SetPosition(Vector2Int newPosition)
        {
            if(_position == newPosition)
            {
                //Debug.LogWarning($"Tried to set the position of agent ${this.GetName()} to his current position. This should be prohibited!");
                return;
            }

            //Debug.Log($"Updated position of {this.GetName()} to {newPosition}");
            Vector2Int oldPosition = _position;
            bool didMove = _manager.OnPositionUpdate(this, newPosition, oldPosition);
            
            if(!didMove) return;
            
            _position = newPosition;
        }
        public void Reset()
        {
            // TODO: Handle HP and Armor here when implemented.

            this.Health.Reset();
            this.ActionPoints = GameConfig.ActionPointStart;
            _endTurnAfter = false;
        }
        
        private Color GetTileColor()
        {
            return _factionIndex switch {
                0 => Color.green,
                1 => Color.red,
                _ => Color.magenta,
            };
        }
        
        private void OnDisplayValueChange()
        {
            _currentAgentChangedEvent?.Raise(this);
        }
    }
}
