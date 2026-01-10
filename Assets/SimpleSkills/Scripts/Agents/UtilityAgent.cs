using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _General;
using _General.Custom_Attributes;
using SimpleSkills.Scripts;
using SimpleSkills.Scripts.RewardProvider;
using SimpleSkills.UtilityAi;
using UnityEngine;

namespace SimpleSkills
{
    public class UtilityAgent : MonoBehaviour, ISkAgent
    {
        [Header("Events")]
        [SerializeField, Required] private SkAgentEvent _currentAgentChangedEvent;
        [SerializeField, Required] private SkillEvent _lastUsedSkillChangedEvent;
        
        [Header("Agent Settings")]
        [SerializeField] private int _maxHealth = 10;
        [SerializeField] private List<UtilityAction> _actions = new List<UtilityAction>();

        [Header("Keys")]
        [SerializeField, Required] private WorldDataKey _actionPointKey;
        [SerializeField, Required] private WorldDataKey _enemiesInMeleeKey;

        private string _agentName;
        private Vector2Int _position;
        private int _actionPoints;

        private CancellationTokenSource _cancelTokenSource;
        
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
        public float MovementRange { get => 4.5f; }
        public string CurrentTurnId { get; private set; }
        
        public IReadOnlyList<SimpleSkill> Skills { get => _actions.Select(action => action.Skill).ToList(); }
        
        private void Start()
        {
            _cancelTokenSource = new CancellationTokenSource();
        }

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
            this.ID = Guid.NewGuid().ToString();
            _agentName = agentName;
            this.FactionIndex = factionIndex;
            this.GameplayManager = manager as SkGameplayManager;
            this.Health = new Attribute<int>(_maxHealth, _maxHealth);
            
            _cancelTokenSource = new CancellationTokenSource();
            
            this.Reset();
        }
        public string GetName() => _agentName;
       
        public void OnTurnStart()
        {
            Debug.Log("Starting Utility Agents turn!");
            
            this.CurrentTurnId = Guid.NewGuid().ToString();
            
            //TODO: ActionPoints should not be handled by the actors themselves
            this.ActionPoints = Mathf.Min(this.ActionPoints + GameConfig.ActionPointTurnRecover, GameConfig.ActionPointMax);
            this.EndTurnAfter = false;
            
            this.SelectAction();
        }
        
        public void EndTurn()
        {
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
            this.Health.Set(Mathf.Max(0, this.Health.Value - damage));
            Debug.Log($"{this.GetName()} received {damage} damage from {damageOrigin.GetName()}.");

            bool willDie = this.Health.Value <= 0;
            if(willDie) this.Die(damageOrigin);

            return willDie;
        }

        public void DealDamage(ISkAgent damageTarget, int damage)
        {
            Debug.Log($"Utility Agents deals damage to {damageTarget.GetName()}");
            bool killedTarget = damageTarget.TakeDamage(this, damage);
            if(killedTarget) this.GameplayManager.ScoreKeeper.AddScore(ScoreType.Kill, this.FactionIndex);;
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

            Debug.Log($"Utility: Updated position of {this.GetName()} to {newPosition}");
            Vector2Int oldPosition = _position;
            _position = newPosition;
            this.GameplayManager.OnPositionUpdate(this, _position, oldPosition);
        }

        private void SelectAction()
        {
            Debug.Log("Utility is selecting Action ---------------------------------");
            List<UtilityAction> possibleActions = _actions.Where(action => action.Skill.ActionPointCost <= this.ActionPoints).ToList();
            
            if(possibleActions.Count <= 0)
            {
                Debug.LogWarning($"UtilityAgent {this.GetName()} does not have any possible actions!");
                this.EndTurn();
                return;
            }

            this.UpdateWorldState();

            int selectedIndex = -1;
            float currentUtilityValue = -1;

            for (int i = 0; i < possibleActions.Count; i++)
            {
                UtilityAction action = possibleActions[i];
                
                float actionUtility = action.GetUtilityValue(this, this.GameplayManager.WorldState);
                //Debug.Log($"Evaluated {action.Skill.Name} with value: {actionUtility}.");
                
                // Actions with higher actions are often more complex, so we do >=
                if(actionUtility < currentUtilityValue) continue;
                selectedIndex = i;
                currentUtilityValue = actionUtility;
            }

            if(selectedIndex <= -1)
            {
                Debug.LogError("Could not select any action. This should be impossible!");
                this.EndTurn();
                return;
            }
            
            UtilityAction selectedAction = possibleActions[selectedIndex];
            this.ExecuteAction(selectedAction.Skill, new Vector2Int(-1,-1));
        }

        private async void ExecuteAction(SimpleSkill skill, Vector2Int boardPosition)
        {
            Debug.Log($"Selected skill is: " + skill);
            
            SkillContext context = new SkillContext(this, this.CurrentTurnId, boardPosition);
            
            if(skill.ActionPointCost > this.ActionPoints) 
            {
                Debug.LogWarning($"UtilityAgent tried to use an action, which required to many ap!");
                this.EndTurn();
                return;
            }

            bool canExecute = await skill.CanExecute(context, _cancelTokenSource.Token);

            if(!canExecute)
            {
                Debug.Log("Can not execute skill!");
                this.EndTurn();
                return ;
            }

            bool didExecute = await skill.ExecuteSkill(context, _cancelTokenSource.Token);
            
            if(!didExecute)
            {
                Debug.Log("Tried and failed to execute skill!");
                this.EndTurn();
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
            else
            {
                this.SelectAction();
            }
        }
        
        private void UpdateWorldState()
        {
            SkBoardManager boardManager = this.GameplayManager.BoardManager;
            float enemyCount = boardManager.GetTilesInRadiusWithQuery(this.Position, 1, tile => tile.ContainedEntity is ISkAgent).Count;
            
            this.GameplayManager.WorldState.SetData(_enemiesInMeleeKey, enemyCount);
            this.GameplayManager.WorldState.SetData(_actionPointKey, (float)this.ActionPoints);
            
            Debug.Log($"Updated enemy count to: " + enemyCount);
        }

        private void OnDisplayValueChange()
        {
            _currentAgentChangedEvent?.Raise(this);
        }
    }
}
