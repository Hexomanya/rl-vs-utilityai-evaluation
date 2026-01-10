using System;
using System.Collections.Generic;
using System.Threading;
using _General;
using SimpleSkills.Scripts;
using SimpleSkills.Scripts.RewardProvider;
using UnityEngine;

namespace SimpleSkills
{
    public class PlayerAgent : MonoBehaviour, ISkAgent
    {
        [Header("Events")]
        [SerializeField] private WorldPositionEvent _tileClickedWorldPositionEvent;
        [SerializeField] private IntegerEvent _skillActionPerformed;
        [SerializeField] private SkAgentEvent _currentAgentChangedEvent;
        [SerializeField] private SkillEvent _lastUsedSkillChangedEvent;
        
        [Header("Agent Settings")]
        [SerializeField] private int _maxHealth = 10;
        [SerializeField] private List<SimpleSkill> _skills = new List<SimpleSkill>();

        private string _agentName;
        private Vector2Int _position;
        private int _actionPoints;

        private CancellationTokenSource _cancelTokenSource;
        private Vector2Int? _selectedPosition;
        
        public string ID { get; private set; }
        public Color TileColor { get => this.GetTileColor(); }
        public SkGameplayManager GameplayManager { get; private set; }
        public Vector2Int Position { get => _position; set => this.SetPosition(value); }
        public int ActionPoints { get => _actionPoints; 
            set 
            {
            _actionPoints = value;
            this.OnDisplayValueChange(); 
            } 
        }
        public int FactionIndex { get; private set; }
        public bool EndTurnAfter { get; set; }
        public Attribute<int> Health { get; private set; }

        public IReadOnlyList<SimpleSkill> Skills { get => _skills; }
        public float MovementRange { get => 4.5f; }
        public string CurrentTurnId { get; private set; }
        
        private void Start()
        {
            _cancelTokenSource = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
        }
        
        public void SetupAgent(string agentName, int factionIndex, IGameplayManager manager)
        {
            this.ID = Guid.NewGuid().ToString();
            _agentName = agentName;
            this.FactionIndex = factionIndex;
            this.GameplayManager = manager as SkGameplayManager;
            this.Health = new Attribute<int>(_maxHealth, _maxHealth);
            
            this.Reset();
        }
        public string GetName() => _agentName;
       
        public void OnTurnStart()
        {
            GameLog.Print("It is my turn!", this);
            
            this.CurrentTurnId = Guid.NewGuid().ToString();
            
            //TODO: ActionPoints should not be handled by the actors themselves
            this.ActionPoints = Mathf.Min(this.ActionPoints + GameConfig.ActionPointTurnRecover, GameConfig.ActionPointMax);
            this.EndTurnAfter = false;
            
            _selectedPosition = null;
            _tileClickedWorldPositionEvent.Subscribe(this.OnWorldPositionSelected);
            _skillActionPerformed.Subscribe(this.OnSkillActionPerformed);
        }
        public void EndTurn()
        {
            _tileClickedWorldPositionEvent.Unsubscribe(this.OnWorldPositionSelected);
            _skillActionPerformed.Unsubscribe(this.OnSkillActionPerformed);
            
            this.GameplayManager.OnTurnDone(this);
        }
        
        public void OnGameEnd(WinLooseSignal result) { }
        
        private void Die(ISkAgent killOriginAgent)
        {
            this.GameplayManager.RemoveFromGame(this);
            this.GameplayManager.ScoreKeeper.AddScore(ScoreType.Death, this.FactionIndex);
            Debug.Log($"{this.GetName()} was killed by {killOriginAgent.GetName()}");
            GameLog.Print($"Was killed by {killOriginAgent.GetName()}.", this);
        }
        
        public bool TakeDamage(ISkAgent damageOrigin, int damage)
        {
            this.Health.Set( Mathf.Max(0, this.Health.Value - damage));
            GameLog.Print($"Received {damage} damage from {damageOrigin.GetName()}.", this);

            bool willDie = this.Health.Value <= 0;
            if(willDie) this.Die(damageOrigin);

            return willDie;
        }

        public void DealDamage(ISkAgent damageTarget, int damage)
        {
            GameLog.Print($"Inflicted {damage} damage to {damageTarget.GetName()}.", this);
            
            bool killedTarget = damageTarget.TakeDamage(this, damage);
            if(killedTarget) this.GameplayManager.ScoreKeeper.AddScore(ScoreType.Kill, this.FactionIndex);
        }

        public void Reset()
        {
            // TODO: Handle HP and Armor here when implemented.

            this.Health.Reset();
            this.ActionPoints = GameConfig.ActionPointStart;
            this.EndTurnAfter = false;
        }

        private Color GetTileColor()
        {
            return this.FactionIndex switch {
                0 => Color.green,
                1 => Color.red,
                _ => Color.magenta,
            };
        }
        
        private void SetPosition(Vector2Int newPosition)
        {
            if(_position == newPosition)
            {
                //Debug.LogWarning($"Tried to set the position of agent ${this.GetName()} to his current position. This should be prohibited!");
                return;
            }

            Debug.Log($"Updated position of {this.GetName()} to {newPosition}");
            Vector2Int oldPosition = _position;
            _position = newPosition;
            this.GameplayManager.OnPositionUpdate(this, _position, oldPosition);
        }

        private void OnWorldPositionSelected(Vector3 selectedPosition)
        {
            _selectedPosition = this.GameplayManager.BoardManager.WorldToBoardPosition(selectedPosition);
        }
        
        private void OnSkillActionPerformed(int index)
        {
            if(index < 0 || index >= _skills.Count)
            {
                Debug.Log($"Received skill index is out of bounds. index:{index}");
                return;
            }

            Vector2Int position = _selectedPosition ?? this.Position;
            this.ExecuteAction(_skills[index], position);
        }
        
        private async void ExecuteAction(SimpleSkill skill, Vector2Int boardPosition)
        {
            Debug.Log($"Execute Action request for {skill.Name} with position {boardPosition}");
            
            SkillContext context = new SkillContext(this, this.CurrentTurnId, boardPosition);
            
            if(skill.ActionPointCost > this.ActionPoints) 
            {
                Debug.Log("Not enough ActionPoints");
                return;
            }

            bool canExecute = await skill.CanExecute(context, _cancelTokenSource.Token);

            if(!canExecute)
            {
                Debug.Log("Can not execute skill!");
                return;
            }

            bool didExecute = await skill.ExecuteSkill(context, _cancelTokenSource.Token);
            
            if(!didExecute)
            {
                Debug.Log("Tried and failed to execute skill!");
                return;
            }
            
            GameLog.Print($"Used skill: {skill.Name}.", this);
            _lastUsedSkillChangedEvent.Raise(skill);
            
            this.ActionPoints -= skill.ActionPointCost;
            bool didEndGame = this.GameplayManager.OnActionTook(this, -1);

            if(this.ActionPoints == 0 || this.EndTurnAfter || didEndGame)
            {
                this.EndTurn();
            }
        }
        
        private void OnDisplayValueChange()
        {
            _currentAgentChangedEvent?.Raise(this);
        }
    }
}
