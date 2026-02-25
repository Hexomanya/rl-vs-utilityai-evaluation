using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _General;
using _General.TypeExtensions;
using ArtificialEnemy;
using KBCore.Refs;
using SimpleSkills.Configs;
using SimpleSkills.Scripts;
using SimpleSkills.Scripts.RewardProvider;
using SimpleSkills.UtilityAi;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SimpleSkills
{
    public class SkGameplayManager : MonoBehaviour, IGameplayManager
    {
        [SerializeField, Anywhere] private SkBoardManager _boardManager;
        [Header("Events")]
        [SerializeField] private SkAgentEvent _currentAgentChangedEvent;
        [SerializeField] private SkAgentEvent _agentTookActionEvent;
        [SerializeField] private GameResultEvent _gameResultEvent;
        
        private WorldState _worldState;
        private readonly SkillUseCounter _skillUseCounter = new SkillUseCounter();

        private readonly Dictionary<string, ISkAgent> _allAgents = new Dictionary<string, ISkAgent>();
        private readonly List<ISkAgent> _deadAgents = new List<ISkAgent>();
        private List<ISkAgent> _initiative = new List<ISkAgent>();
        private ISkAgent _currentAgent;
        private Coroutine _gameLoopCoroutine;
        private bool _isWaitingOnTurn;
        private RunConfig _runConfig;
        private GameplayState _state = GameplayState.Stopped;
        private int _currentRound = 0;

        public event Action<string> OnGameEnded;

        public string GameManagerID = "";

        public SkBoardManager BoardManager { get => _boardManager; }
        public int CurrentRound { get => _currentRound; }
        public RunConfig RunConfig { get => _runConfig; }
        public WorldState WorldState { get => _worldState; }
        public SkillUseCounter SkillUseCounter { get => _skillUseCounter; }
        public ScoreKeeper ScoreKeeper { get; private set; }
        private ISkAgent CurrentAgent
        {
            get => _currentAgent;
            set
            {
                _currentAgent = value;
                _currentAgentChangedEvent.Raise(_currentAgent);
            }
        }

        public void OnStart(ScoreKeeper scoreKeeper, RunConfig config)
        {
            this.GameManagerID = GUID.Generate().ToString();
            
            Debug.Log("On Start called");
            _runConfig = config;
            _worldState = new WorldState();
            this.ScoreKeeper = scoreKeeper;
            
            if(_state == GameplayState.Stopped)
            {
                this.Setup();
            }

            _state = GameplayState.Running;
            this.CreateNewInitiative();
            this.CurrentAgent = _initiative[0];

            if(_gameLoopCoroutine != null)
            {
                StopCoroutine(_gameLoopCoroutine);
                _gameLoopCoroutine = null;
            }

            Debug.Log("Starting GameLoop");
            _gameLoopCoroutine = StartCoroutine(this.GameLoop());
        }
        
        public Dictionary<string, int> OnStop()
        {
            if(_gameLoopCoroutine != null)
            {
                StopCoroutine(_gameLoopCoroutine);
                _gameLoopCoroutine = null;
            }

            _state = GameplayState.Stopped;

            this.Reset();

            return new Dictionary<string, int>();
        }
        
        // Was TakeAction, but was repurposed to not have to rewrite the other manager.
        public bool OnActionTook(IAgent actionAgent, int actionIndex)
        {
            if(actionAgent.ID != this.CurrentAgent.ID)
            {
                Debug.LogError("Mismatch between GameplayManager and Agent detected!");
            }

            if(actionAgent is ISkAgent agent) _agentTookActionEvent.Raise(agent);
            
            int winnerFactionIndex = this.CheckWinner();
            if(winnerFactionIndex == -1) return false;

            if(StateManager.IsInSurveyMode) this.StartCoroutine(this.WaitThenExecute(1f, () => this.EndGame(winnerFactionIndex, false)));
            else this.EndGame(winnerFactionIndex, false);
            
            return true;
        }
        
        private IEnumerator WaitThenExecute(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
        
        private void EndGame(int winnerFactionIndex, bool wasForced)
        {
            foreach (ISkAgent agent in _allAgents.Values)
            {
                if(winnerFactionIndex >= 0)
                {
                    agent.OnGameEnd(agent.FactionIndex == winnerFactionIndex ? WinLooseSignal.Win : WinLooseSignal.Loose);
                }
                else
                {
                    agent.OnGameEnd(WinLooseSignal.Draw);   
                }
            }
            
            for (int i = 0; i < _runConfig.Factions.Count(); i++)
            {
                ScoreType scoreType = winnerFactionIndex <= -1 ? ScoreType.Draw : (winnerFactionIndex == i ? ScoreType.Win : ScoreType.Lose);
                this.ScoreKeeper.AddScore(scoreType, i);
            }
            
            _skillUseCounter.CountSkillUse("Episode End", wasForced ? "Forced" : "Normal");

            _state = GameplayState.WinEndState;
            _skillUseCounter.ReportAndClear();
            
            // Survey player should always be 0
            GameResult result = winnerFactionIndex switch
            {
                < 0 => GameResult.Draw,
                0 => GameResult.Win,
                _ => GameResult.Loose,
            };
            _gameResultEvent.Raise(result);

            OnGameEnded?.Invoke("SK");
        }

        public void OnTurnDone(IAgent doneAgent, int stayAtIndex = -1)
        {
            if(doneAgent.ID != this.CurrentAgent.ID)
            {
                Debug.LogError("Mismatch between GameplayManager and agent detected!");
            }

            if(doneAgent is not ISkAgent mlSkAgent)
            {
                Debug.LogError("Used wrong kind of agent!");
                return;
            }
            
            if(stayAtIndex == -1)
            {
                int currentIndex = _initiative.IndexOf(mlSkAgent);
                int nextIndex = (currentIndex + 1) % _initiative.Count;
                this.CurrentAgent = _initiative[nextIndex];

                if(nextIndex == 0) _currentRound++;
            }
            else
            {
                this.CurrentAgent = _initiative[stayAtIndex];
            }

            if(_currentRound >= _runConfig.MaxRounds) this.EndGame(-1, true);
            
            _isWaitingOnTurn = false;
        }

        //NOT NEEDED FOR SK
        public GameState GetGameState(int playerIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromGame(IAgent agent)
        {
            if(agent is not ISkAgent mlSkAgent)
            {
                throw new ArgumentException("Received non Mlskagent");
            }

            int agentIndex = _initiative.IndexOf(mlSkAgent);
            
            bool didRemove = _initiative.Remove(mlSkAgent);
            if(!didRemove)
            {
                string agentNames = string.Join(", ", _initiative.Select(printAgent => printAgent.GetName()));
                Debug.LogError($"Tried to remove agent with name {agent.GetName()} from Initiative, that was not in it. Initiative is: \n {agentNames}");
                return;
            }
            
            _boardManager.ClearTile(mlSkAgent.Position);
            
            if(mlSkAgent.ID == this.CurrentAgent.ID)
            {
                Debug.LogWarning("Agent somehow killed himself, this might mess with the turn order!");
                this.OnTurnDone(agent, agentIndex);
            }
            
            _deadAgents.Add(mlSkAgent);
        }
        
        public bool OnPositionUpdate(IAgent movedAgent, Vector2Int newPosition, Vector2Int oldPosition)
        {
            if(movedAgent is not ISkAgent skAgent)
            {
                throw new ArgumentException("Received non ISkAgent");
            }

            if(movedAgent is ISkAgent agent)_currentAgentChangedEvent.Raise(agent);
            return _boardManager.MoveAgent(skAgent, newPosition, oldPosition);
        }
        
        private void Setup()
        {
            Debug.Log($"Setting Up (Id: {this.GameManagerID})");
            _boardManager.Initialize(_runConfig.TestMapIndex, this.GameManagerID);
            this.CreateAgents();
            this.MoveTeamsToStartPos();
        }

        private void CreateAgents()
        {
            Debug.Log("Creating Agents. This should only happen once in the lifecycle!");

            List<ISkAgent> preplacedAgents = new List<ISkAgent>();
            
            for (int k = 0; k < this.transform.childCount; k++)
            {
                GameObject childObject = this.transform.GetChild(k).gameObject;
                if(childObject.TryGetComponent(out ISkAgent preplacedAgent)) {
                    preplacedAgents.Add(preplacedAgent); 
                }
            }
            
            _allAgents.Clear();
            _deadAgents.Clear();

            for (int i = 0; i < _runConfig.Factions.Count; i++)
            {
                Faction currentFaction = _runConfig.Factions[i];
                List<string> factionNames = new List<string>(_runConfig.AgentNames);

                foreach (GameObject currentSetupAgent in currentFaction.TeamSetup)
                {
                    if(! currentSetupAgent.TryGetComponent(out ISkAgent skAgent)) {
                        Debug.LogError("Team Setup contained non SkAgent!");
                        continue;
                    }

                    ISkAgent agent = null;
                    for (int k = 0; k < preplacedAgents.Count; k++)
                    {
                        if(preplacedAgents[k].GetType() != skAgent.GetType()) continue;

                        agent = preplacedAgents[k];
                        preplacedAgents.RemoveAt(k);
                        break;
                    }
                    
                    if(agent is null)
                    {
                        Debug.LogError("Could not find pre placed agent of matching type!");
                        return;
                    }
                    
                    string factionName = currentFaction.Name;

                    int nameIndex = Random.Range(0, factionNames.Count);
                    string randomAgentName = factionNames[nameIndex];
                    factionNames.RemoveAt(nameIndex);
                    
                    string agentName = $"{factionName}_{randomAgentName}";
                    
                    agent.SetupAgent(agentName, i, this);
                    _allAgents.Add(agent.ID, agent);
                }
            }
        }

        private void MoveTeamsToStartPos()
        {
            for (int i = 0; i < _runConfig.Factions.Count; i++)
            {
                Vector2Int randomPos = new Vector2Int(Random.Range(0, _boardManager.Width), Random.Range(0, _boardManager.Height));
                
                if(!_boardManager.TryGetFreePosAround(randomPos, out Vector2Int factionStartPos, 5))
                {
                    Debug.LogWarning($"Could not find any safe position around {randomPos}. This should not happen!");
                    factionStartPos = Vector2Int.zero;
                }

                List<ISkAgent> factionAgents = _allAgents.Values.Where(agent => agent.FactionIndex == i).ToList();

                foreach (ISkAgent agent in factionAgents)
                {
                    if(!_boardManager.TryGetFreePosAround(factionStartPos, out Vector2Int agentStartPos, 5))
                    {
                        Debug.LogWarning($"Could not find any safe position around {factionStartPos}. This should not happen!");
                        factionStartPos = Vector2Int.zero;
                    }
                    
                    agent.Position = agentStartPos;
                }
            }
        }
        
        private IEnumerator GameLoop()
        {
            while (_state == GameplayState.Running)
            {
                _isWaitingOnTurn = true;
                this.CurrentAgent.OnTurnStart();

                yield return new WaitUntil(() => !_isWaitingOnTurn);

                if(_state == GameplayState.WinEndState)
                {
                    yield return new WaitForSeconds(0.01f); // Small delay between games
                    this.NewGame();
                }
                
                yield return new WaitForSeconds(_runConfig.RoundDelay);
            }
        }

        private void Reset()
        {
            _state = GameplayState.Stopped;
            _boardManager.Reset();
            this.CreateNewInitiative();
            
            foreach (ISkAgent agent in _allAgents.Values)
            {
                agent.Reset();
            }
        }
        
        private void NewGame()
        {
            this.Reset();
            _boardManager.NewGame();
            this.MoveTeamsToStartPos();
            this.CreateNewInitiative();
            _currentRound = 0;
            _state = GameplayState.Running;
        }
        
        //TODO: We are currently not handling the case where we have draw, because everyone is dead
        private int CheckWinner()
        {
           
            List<int> aliveFactions = _initiative.Select(agent => agent.FactionIndex).ToList();

            if(aliveFactions.Count > 1)
            {
                return -1;
            }

            return aliveFactions[0];
        }
        
        private void CreateNewInitiative()
        {
            _initiative.Clear();
            _deadAgents.Clear();

            foreach (ISkAgent agent in _initiative)
            {
                agent.CancelTasks();
            }

            _initiative = new List<ISkAgent>(_allAgents.Values);
            _initiative.Shuffle();

            this.CurrentAgent = _initiative[0];
        }
        
        public Vector2 GetMultipleGameOffset()
        {
            Vector2 minOffset = new Vector2(_boardManager.Width, _boardManager.Height) * BoardConfig.CellSize;
            return minOffset * (minOffset.x * 0.5f);
        }
        
        public async Task<PathToTarget> GetClosestEnemyAgent(Vector2Int originPosition, List<int> friendlyFactionIndices, CancellationToken cancelToken, bool excludeOriginPosition = true)
        {
            List<ISkAgent> possibleAgents = _initiative.Where(agent =>
            {
                bool isEnemy = !friendlyFactionIndices.Contains(agent.FactionIndex);
                if (!excludeOriginPosition) return isEnemy;
                
                bool isSamePosition = agent.Position.Equals(originPosition);
                return !isSamePosition;
            }).OrderBy(agent => _boardManager.TileToWorldDistance(originPosition, agent.Position)).ToList();
            
            foreach (ISkAgent agent in possibleAgents)
            {
                if (agent.Position.Equals(originPosition))
                {
                    Debug.LogError("Possible agent is on origin position! This should be impossible!");
                    continue;
                }
                
                PathfindingRequest request = new PathfindingRequest(originPosition, agent.Position, true, _boardManager);
                await _boardManager.GetPath(request, cancelToken);
                
                if(request.Result.DidFindPath) return new PathToTarget {
                    Target = agent,
                    PathDescription = request.Result,
                };
            }
            
            //Debug.LogWarning("Could not find any agents or a path!");
            return new PathToTarget();
        }
        
        public static string ToPrettyString(IList<Vector2Int> list)
        {
            if (list == null || list.Count == 0)
                return "[]";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('[');

            for (int i = 0; i < list.Count; i++)
            {
                Vector2Int v = list[i];
                sb.Append('(');
                sb.Append(v.x);
                sb.Append(',');
                sb.Append(v.y);
                sb.Append(')');

                if (i < list.Count - 1)
                    sb.Append(", ");
            }

            sb.Append(']');
            return sb.ToString();
        }
    }
}
