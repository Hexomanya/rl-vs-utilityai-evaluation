using _General;
using SimpleSkills.Scripts.RewardProvider;
using UnityEngine;

namespace ArtificialEnemy
{
    public class RandomAgent : MonoBehaviour, IAgent
    {
        private IGameplayManager _gameplayManager;
        private string _id;
        private int _playerIndex;

        public void SetupAgent(string id, int playerIndex, int factionIndex, IGameplayManager manager)
        {
            _id = id;
            _playerIndex = playerIndex;
            _gameplayManager = manager;

            Debug.Log("Successfully setup agent!");
        }
        //TODO: Repair
        public void SetupAgent(string agentName, int factionIndex, IGameplayManager manager)
        {
            throw new System.NotImplementedException();
        }

        public string ID { get; }
        
        public string GetName()
        {
            return _id;
        }
        public void OnTurnStart()
        {
            GameState gameState = _gameplayManager.GetGameState(_playerIndex);
            int bestMove = this.ComputeBestMove(gameState);
            _gameplayManager.OnActionTook(this, bestMove);

            _gameplayManager.OnTurnDone(this);
        }

        public void EndTurn()
        {
        }
        public void OnGameEnd(WinLooseSignal result)
        {
        }

        private int ComputeBestMove(GameState gameState)
        {
            if(gameState.PossibleActions.Count == 0)
            {
                return -1;
            }

            int randMove = Random.Range(0, gameState.PossibleActions.Count);
            return gameState.PossibleActions[randMove];
        }
    }
}
