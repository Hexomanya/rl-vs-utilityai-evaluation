using System;
using System.Collections;
using System.Collections.Generic;
using _General;
using ArtificialEnemy;
using SimpleSkills.Scripts.RewardProvider;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TicTacToe.Scripts
{
    public class TTTGameplayManager : MonoBehaviour, IGameplayManager
    {
        [SerializeField] private TTTBoardManager _gameBoard;

        [SerializeField] private MlTttAgent _agentOne;
        [SerializeField] private MlTttAgent _agentTwo;
        [SerializeField] private RandomAgent _randomAgent;
        private readonly List<IAgent> _players = new List<IAgent>();
        private readonly Dictionary<string, int> _playerWins = new Dictionary<string, int>();
        private int _currentPlayer;
        private Coroutine _gameLoopCoroutine;
        private bool _isWaitingOnTurn;
        private RunConfig _runConfig;

        public event Action<string> OnGameEnded;

        private GameplayState _state = GameplayState.Stopped;

        private void Reset()
        {
            _state = GameplayState.Stopped;
            _currentPlayer = _runConfig.RandomizePlayerOrder ? Random.Range(0, _players.Count) : 0;
            _gameBoard.Reset();
        }

        public void OnStart(ScoreKeeper scoreKeeper, RunConfig config)
        {
            _runConfig = config;

            if(!_gameBoard)
            {
                Debug.LogError("Gameboard can't be empty!");
                return;
            }

            if(_state == GameplayState.Stopped)
            {
                this.Setup();
            }

            Debug.Log("Setting state to running.");
            _state = GameplayState.Running;
            _currentPlayer = _runConfig.RandomizePlayerOrder ? Random.Range(0, _players.Count) : 0;

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

            return _playerWins;
        }
        //TODO: Repair
        public bool OnActionTook(IAgent agent, int actionIndex)
        {
            throw new NotImplementedException();
        }
        public void OnTurnDone(IAgent agent, int stayAtIndex = -1)
        {
            throw new NotImplementedException();
        }

        public bool OnActionTook(int playerIndex, int bestMoveIndex)
        {
            if(playerIndex != _currentPlayer)
            {
                Debug.LogError("Mismatch between GameplayManager and Player detected!");
            }

            if(bestMoveIndex == -1)
            {
                Debug.LogWarning($"{_players[playerIndex].GetName()} did not find a move to take.");
                return false;
            }

            _gameBoard.AssignTileToPlayer(playerIndex, bestMoveIndex);
            List<int> boardState = _gameBoard.GetGenericState();

            int winnerIndex = TicTacToeUtils.CheckWinner(boardState);

            if(winnerIndex != -1)
            {
                int loserIndex = winnerIndex == 0 ? 1 : 0;

                _players[winnerIndex].OnGameEnd(WinLooseSignal.Win);
                _players[loserIndex].OnGameEnd(WinLooseSignal.Loose);

                string winnerName = _players[winnerIndex].GetName();

                if(_playerWins.TryGetValue(winnerName, out int wins))
                {
                    _playerWins[winnerName] = wins + 1;
                }
                else
                {
                    _playerWins[winnerName] = 1;
                }

                _state = GameplayState.WinEndState;
                return true;
            }

            bool isGameStuck = !TicTacToeUtils.AreThereFreeTiles(boardState);

            if(!isGameStuck)
            {
                return true;
            }

            foreach (IAgent player in _players)
            {
                player.OnGameEnd(0);
            }

            _state = GameplayState.WinEndState;
            return true;

        }

        public void OnTurnDone(int playerIndex)
        {
            if(playerIndex != _currentPlayer)
            {
                Debug.LogError("Mismatch between GameplayManager and Player detected!");
            }

            _players[_currentPlayer].EndTurn(); //Reset Variables

            //Set new Player
            _currentPlayer = (_currentPlayer + 1) % _players.Count;
            _isWaitingOnTurn = false;
        }

        public GameState GetGameState(int playerIndex)
        {
            List<int> state = _gameBoard.GetGenericState();

            List<int> legalMoves = new List<int>();

            for (int i = 0; i < state.Count; i++)
            {
                if(state[i] == -1)
                {
                    legalMoves.Add(i);
                }
            }

            return new GameState {
                State = state,
                PossibleActions = legalMoves,
            };
        }

        public bool OnPositionUpdate(IAgent movedAgent, Vector2Int newPosition, Vector2Int oldPosition) { return true;}
        public void RemoveFromGame(IAgent mlSkAgent)
        {
            throw new NotImplementedException();
        }
        public Vector2 GetMultipleGameOffset()
        {
            return new Vector2(10, 10);
        }


        // --- SimStateManagement ---------------------------------
        private void Setup()
        {
            Debug.Log("Setting Up");

            if(_state != GameplayState.Stopped)
            {
                Debug.LogWarning("Can not setup manager, when it is not stopped!");
                return;
            }

            if(_agentOne is null || _agentTwo is null)
            {
                Debug.LogWarning("Can not setup manager, when the players are not set correctly.");
                return;
            }

            string nameA = _runConfig.AgentNames[Random.Range(0, _runConfig.AgentNames.Count)];
            string nameB = _runConfig.AgentNames[Random.Range(0, _runConfig.AgentNames.Count)];
            _agentOne.SetupAgent(nameA, 0, 0, this);

            if(_randomAgent is null)
            {
                _agentTwo.SetupAgent(nameB, 1, 1, this);
            }

            if(_randomAgent is not null)
            {
                _randomAgent.SetupAgent(nameB, 1, 1, this);
            }

            _players.Add(_agentOne);

            if(_randomAgent is null)
            {
                _players.Add(_agentTwo);
            }

            if(_randomAgent is not null)
            {
                _players.Add(_randomAgent);
            }

            Debug.Log("Player count is: " + _players.Count);

            _gameBoard.Initialize();
        }

        private IEnumerator GameLoop()
        {
            while (_state == GameplayState.Running)
            {
                //Debug.Log($"{_players[_currentPlayer].GetName()}s turn!");
                _isWaitingOnTurn = true;
                _players[_currentPlayer].OnTurnStart();

                yield return new WaitUntil(() => !_isWaitingOnTurn);

                if(_state == GameplayState.WinEndState)
                {
                    yield return new WaitForSeconds(0.01f); // Small delay between games
                    this.Reset();
                    _state = GameplayState.Running;
                }

                yield return new WaitForSeconds(_runConfig.RoundDelay);
            }
        }
    }
}
