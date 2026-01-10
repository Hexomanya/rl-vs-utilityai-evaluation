using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _General;
using _General.Custom_Attributes;
using KBCore.Refs;
using SimpleSkills.Scripts;
using SimpleSkills.Scripts.RewardProvider;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SimpleSkills
{
    [RequireComponent(typeof(GridSensorComponent)), RequireComponent(typeof(BehaviorParameters))]
    public class MlSkAgent : Agent, ISkAgent
    {
        [Header("Components")]
        [SerializeField, Self] private GridSensorComponent _gridSensor;
        [SerializeField, Self] private BehaviorParameters _behaviorParameters;

        [Header("Configuration")]
        [SerializeField] private int _maxHealth = 10;
        [SerializeField] private List<SimpleSkill> _skills = new List<SimpleSkill>();
        [SerializeField] private bool _usesPositionSelection = false;
        [SerializeField] private bool _useTwoPositionBranches = false;
        [SerializeField] private bool _usesRewardShaping = false;

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

        // ML-Agents
        private RewardProvider _rewardProvider;
        private GridObservator _gridObservationSensor;
        private readonly Dictionary<int, bool[]> _actionMask = new Dictionary<int, bool[]>();
        private float _currentRetries;

        // Async
        private CancellationTokenSource _cancelTokenSource;
        private SimpleSkill _lastUsedSkill;

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
        public bool UsesRewardShaping { get => _usesRewardShaping; }
        public IReadOnlyList<SimpleSkill> Skills { get => _skills; }
        public float MovementRange { get => 4.5f; }
        
        // Interface Implementations
        public Color TileColor { get => this.GetTileColor(); }
        
        private void OnValidate()
        {
            this.ValidateRefs();
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
            this.CancelTasks();
            
            _id = Guid.NewGuid().ToString();
            _agentName = agentName;
            _factionIndex = factionIndex;
            this.Health = new Attribute<int>(_maxHealth, _maxHealth);
            
            _manager = manager as SkGameplayManager;
            _rewardProvider = new GranularRewardProvider(this, _manager);
            _gridObservationSensor = _gridSensor.GetObservationVisualSensor; // Is here, because it is still null in awake
            
            _behaviorParameters.TeamId = _factionIndex + 1;
            
            _cancelTokenSource = new CancellationTokenSource();
            
            Debug.Log($"{agentName} was assigned id: {_behaviorParameters.TeamId}");
            
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
            _currentRetries = 0;
            _endTurnAfter = false;
            
            this.RequestSkillAction();
        }
        
        private async Task PrecomputeActionMask()
        {
            _actionMask.Clear();
            _actionMask.Add(ActionConfig.ActionIndex, new bool[_skills.Count]);
            
            if(_usesPositionSelection)
            {
                int firstBranchSize = _useTwoPositionBranches
                    ? ObservationConfig.SkillPositionDiameter
                    : ObservationConfig.SkillPositionDiameter * ObservationConfig.SkillPositionDiameter;
                
                _actionMask.Add(ActionConfig.PositionSelectionXIndex, new bool[firstBranchSize]);
                if(_useTwoPositionBranches) _actionMask.Add(ActionConfig.PositionSelectionYIndex, new bool[ObservationConfig.SkillPositionDiameter]);
            }
            
            // Skills
            SkillContext filterContext = new SkillContext(this, this.CurrentTurnId);
            
            if(_cancelTokenSource == null) return;
            CancellationToken safeToken = _cancelTokenSource.Token;
            
            for (int i = 0; i < _skills.Count; i++)
            {
                bool enoughActionPoints = _skills[i].ActionPointCost <= this.ActionPoints;
                bool canUseSkill = enoughActionPoints && await _skills[i].CanExecute(filterContext, safeToken); //TODO: We are doing double work, because we call CanExecute later still
                _actionMask[ActionConfig.ActionIndex][i] = canUseSkill;
            }

            for (int i = _actionMask[ActionConfig.ActionIndex].Length-1; i >= 0; i--)
            {
                bool isMasked = !_actionMask[ActionConfig.ActionIndex][i];
                if(!isMasked) break;
                if(i != 0) continue;

                Debug.LogWarning("Would have masked everything!");
                _actionMask[ActionConfig.ActionIndex][i] = true;
            }
            
            if(!_usesPositionSelection) return;

            //Positions
            //Filter out of bounds index
            if(!_useTwoPositionBranches) this.MaskPositionsOnSingleBranch(_actionMask);
            else this.MaskPositionsOnTwoBranches(_actionMask);
        }
        
        private void MaskPositionsOnSingleBranch(Dictionary<int, bool[]> actionMask)
        {
            for (int i = 0; i < actionMask[ActionConfig.PositionSelectionXIndex].Length; i++)
            {
                Vector2Int position = OneDimUtil.GetPosition(i, ObservationConfig.SkillPositionDiameter, ObservationConfig.SkillPositionDiameter);
                int centeredX = position.x - (ObservationConfig.SkillPositionDiameter - 1) / 2;
                int centeredY = position.y - (ObservationConfig.SkillPositionDiameter - 1) / 2;
                
                Vector2Int centeredOffset = new Vector2Int(centeredX, centeredY);
                Vector2Int actualPosition = _position + centeredOffset;
            
                bool isInBounds = _manager.BoardManager.IsInBounds(actualPosition);
                    
                actionMask[ActionConfig.PositionSelectionXIndex][i] = isInBounds && !_manager.BoardManager.GetTileAt(actualPosition).ContainsObstruction();
            }
        }
        
        private void MaskPositionsOnTwoBranches(Dictionary<int, bool[]> actionMask)
        {
            for (int x = 0; x < actionMask[ActionConfig.PositionSelectionXIndex].Length; x++)
            {
                int centeredXOffset = x - (ObservationConfig.SkillPositionDiameter - 1) / 2;
                Vector2Int boardPos = new Vector2Int(_position.x + centeredXOffset, _position.y);
                actionMask[ActionConfig.PositionSelectionXIndex][x] = _manager.BoardManager.IsInBounds(boardPos);
            }
            
            for (int y = 0; y < actionMask[ActionConfig.PositionSelectionYIndex].Length; y++)
            {
                int centeredYOffset = y - (ObservationConfig.SkillPositionDiameter - 1) / 2;
                Vector2Int boardPos = new Vector2Int(_position.x, _position.y + centeredYOffset);
                actionMask[ActionConfig.PositionSelectionYIndex][y] = _manager.BoardManager.IsInBounds(boardPos);
            }
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            base.WriteDiscreteActionMask(actionMask);
            
            //Key Index corresponds to branch index
            foreach (int key in _actionMask.Keys)
            {
                for (int i = 0; i < _actionMask[key].Length; i++)
                {
                    actionMask.SetActionEnabled(key, i, _actionMask[key][i]);
                }
            }
        }

        public async void RequestSkillAction()
        {
            float[,,] actorGrids = _manager.BoardManager.ComposeActorGrids(_position, _factionIndex);
            _gridObservationSensor.SetGrids(actorGrids);

            await this.PrecomputeActionMask();
       
            RequestDecision();
        }
        
        public void EndTurn()
        {
            // Debug.Log($"End of {_id}s turn! ------------------------");
            _manager.OnTurnDone(this);
        }
        
        public void OnGameEnd(WinLooseSignal result)
        {
            switch (result)
            {
                case WinLooseSignal.Win:
                    _rewardProvider.OnDidWin();
                    break;

                case WinLooseSignal.Loose:
                    _rewardProvider.OnDidLoose();
                    break;

                case WinLooseSignal.Draw:
                    _rewardProvider.OnDidDraw();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
            
            Debug.Log("Ending Episode");
            EndEpisode();
        }

        private void Die(ISkAgent killOriginAgent)
        {
            _manager.RemoveFromGame(this);
            _manager.ScoreKeeper.AddScore(ScoreType.Death, this.FactionIndex);
            Debug.Log($"{this.GetName()} was killed by {killOriginAgent.GetName()}");
            GameLog.Print($"Was killed by {killOriginAgent.GetName()}.", this);
            _rewardProvider.OnDidDie();
        }

        public bool TakeDamage(ISkAgent damageOrigin, int damage)
        {
            if(this.Health.Value <= 0)
            {
                Debug.LogError($"Agent {this.GetName()} is already dead and can not receive more damage!");
                return false;
            }
            
            this.Health.Set(Mathf.Max(0, this.Health.Value - damage));
            Debug.Log($"{this.GetName()} received {damage} damage from {damageOrigin.GetName()}.");
            _rewardProvider.OnDidReceiveDamage(damageOrigin, damage);

            bool willDie = this.Health.Value <= 0;
            if(willDie) this.Die(damageOrigin);

            return willDie;
        }
        
        public void DealDamage(ISkAgent damageTarget, int damage)
        {
            bool killedTarget = damageTarget.TakeDamage(this, damage);
            _rewardProvider.OnDidDealDamage(damageTarget, damage);

            if(killedTarget)
            {
                _manager.ScoreKeeper.AddScore(ScoreType.Kill, this.FactionIndex);
                _rewardProvider.OnDidKill(damageTarget);
            }
        }
        
        public override void CollectObservations(VectorSensor sensor)
        {
            base.CollectObservations(sensor);
            sensor.AddObservation(this.ActionPoints / GameConfig.ActionPointMax);
            sensor.AddObservation(this.Health.Value / this.Health.MaxValue);
        }
        
        public override void OnActionReceived(ActionBuffers actions)
        {
            //Debug.Log($"Decision received: {JsonConvert.SerializeObject(actions)}");
            //TODO: Maybe check if ActionBuffer conforms to expected form
            
            // Skill Selection
            SimpleSkill skill = this.ParseSelectedSkill(actions);
            if(skill is null)
            {
                Debug.LogError($"Could not parse selected Skill! DiscreteActionBuffer: {actions.DiscreteActions.ToString()}");
                this.RetryGetAction();
                return;
            }
            
            //Position Selection
            Vector2Int? selectedPosition = this.ParseSelectedPosition(actions);
            if(_usesPositionSelection && selectedPosition is null)
            {
                this.RetryGetAction();
                return;
            }
            
            
            this.ExecuteAction(skill, selectedPosition);
        }

        private SimpleSkill ParseSelectedSkill(ActionBuffers actions)
        {
            if(ActionConfig.ActionIndex < 0 || ActionConfig.ActionIndex >= actions.DiscreteActions.Length)
            {
                Debug.LogError($"ActionIndex is out of bounds! ActionIndex: {ActionConfig.ActionIndex}, DiscreteActionsCount: {actions.DiscreteActions.Length}");
                return null;
            }
            
            int selectedSkillId = actions.DiscreteActions[ActionConfig.ActionIndex];
            if(selectedSkillId < 0 || selectedSkillId >= _skills.Count)
            {
                Debug.LogWarning($"Skill selection out of bounds: {selectedSkillId}.");
                return null;
            }
            
            return _skills[selectedSkillId];
        }

        private Vector2Int? ParseSelectedPosition(ActionBuffers actions)
        {
            if(!_usesPositionSelection) return null;


            Vector2Int positiveOffsetPosition;

            if(!_useTwoPositionBranches)
            {
                int receivedPositionIndex = actions.DiscreteActions[ActionConfig.PositionSelectionXIndex];
                positiveOffsetPosition = OneDimUtil.GetPosition(
                    receivedPositionIndex,
                    ObservationConfig.SkillPositionDiameter,
                    ObservationConfig.SkillPositionDiameter
                );
            } else
            {
                int receivedXPosition = actions.DiscreteActions[ActionConfig.PositionSelectionXIndex];
                int receivedYPosition = actions.DiscreteActions[ActionConfig.PositionSelectionYIndex];
                positiveOffsetPosition = new Vector2Int(receivedXPosition, receivedYPosition);
            }
            
            //The input ranges from 0 -> ConfigLimit. To allow for negative Movement we have to offset them;
            Vector2Int correctedOffsetPosition = new Vector2Int(
                positiveOffsetPosition.x -= (ObservationConfig.SkillPositionDiameter - 1) / 2,
                positiveOffsetPosition.y -= (ObservationConfig.SkillPositionDiameter - 1) / 2
            );
            
            Vector2Int boardPosition = _position + correctedOffsetPosition;
            
            SkBoardManager board = _manager.BoardManager;
            if(!board.IsInBounds(boardPosition))
            {
                Debug.Log($"Agent selected a position outside the map. This should have been masked!: {boardPosition}.");
                return null;
            }

            return boardPosition;
        }
        
        private async void ExecuteAction(SimpleSkill skill, Vector2Int? boardPosition)
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
                _manager.SkillUseCounter.CountSkillUse("SkillFailed", skill.Name);
                this.RetryGetAction();
                return;
            }
            
            if(this.GameplayManager.RunConfig.IsTestRun)
            {
                Debug.Log($"Agent {this.GetName()} successfully used skill {skill.Name}.");
            }
            
            _manager.SkillUseCounter.CountSkillUse("SkillUse", skill.Name);
            _lastUsedSkillChangedEvent.Raise(skill);
            GameLog.Print($"Used skill: {skill.Name}.", this);
            
            this.ActionPoints -= skill.ActionPointCost;
            bool didEndGame = _manager.OnActionTook(this, -1);

            if(this.ActionPoints == 0 || _endTurnAfter || didEndGame)
            {
                this.EndTurn();
                return;
            }
            
            this.RequestSkillAction();
        }

        private void RetryGetAction()
        {
            //Debug.LogWarning("Retrying actions should not happen!");
            _currentRetries++;
            //this.AddAdjustedReward(_wrongInputPenalty);

            if(_currentRetries < _manager.RunConfig.MaxRetries)
            {
                this.RequestSkillAction();
                return;
            }
            
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
            _rewardProvider.OnDidMove(_manager.BoardManager.TileToWorldDistance(oldPosition, newPosition));
        }
        public void Reset()
        {
            // TODO: Handle HP and Armor here when implemented.

            this.Health.Reset();
            _currentRetries = 0;
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
