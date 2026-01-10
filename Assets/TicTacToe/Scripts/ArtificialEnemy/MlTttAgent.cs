using _General;
using SimpleSkills.Scripts.RewardProvider;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace ArtificialEnemy
{
    public class MlTttAgent : Agent, IAgent
    {
        private IGameplayManager _gameplayManager;
        private string _id;
        private int _playerIndex;

        private int _retryCounter;

        public void SetupAgent(string id, int playerIndex, int factionIndex, IGameplayManager manager)
        {
            _id = id;
            _playerIndex = playerIndex;
            _gameplayManager = manager;

            Debug.Log("Successfully setup agent!");
        }

        public string ID { get; }
        public void SetupAgent(string agentName, int factionIndex, IGameplayManager manager)
        {
            throw new System.NotImplementedException();
        }
        public string GetName()
        {
            return _id;
        }
        public void OnTurnStart()
        {
            //Debug.Log($"Start of {_id}s turn!");
            _retryCounter = 0;
            RequestDecision();
        }

        public void EndTurn() { }

        public void OnGameEnd(WinLooseSignal result)
        {
            SetReward((int) result);
            EndEpisode();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if(_gameplayManager is null)
            {
                Debug.LogWarning("CollectObservations: _gameplayManager is null!");
                return;
            }

            GameState gameState = _gameplayManager.GetGameState(_playerIndex);

            foreach (int fieldValue in gameState.State)
            {
                sensor.AddObservation(fieldValue);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            //Debug.Log($"Decision received: {JsonConvert.SerializeObject(actions.DiscreteActions)}");

            GameState state = _gameplayManager.GetGameState(_playerIndex);
            int action = actions.DiscreteActions[0];

            if(state.PossibleActions.Contains(action))
            {
                //Debug.Log($"{_id} takes a move!");
                _gameplayManager.OnActionTook(this, action);
            }
            else if(_retryCounter < 10)
            {
                //Debug.Log($"{_id} retries!");
                _retryCounter++;
                RequestDecision();
                return;
            }
            else
            {
                Debug.Log("Could not find legal action within 10 tries. Continueing");
            }

            //Debug.Log($"End of {_id}s turn!");
            _gameplayManager.OnTurnDone(this);
        }
    }
}
