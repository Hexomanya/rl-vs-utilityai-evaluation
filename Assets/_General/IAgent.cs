using _General;
using SimpleSkills.Scripts.RewardProvider;
using UnityEngine;

namespace ArtificialEnemy
{
    public interface IAgent
    {
        public string ID { get; }
        public void SetupAgent(string agentName, int factionIndex, IGameplayManager manager);
        public string GetName();
        public void OnTurnStart();
        public void EndTurn();
        public void OnGameEnd(WinLooseSignal result); //-1 = lost, 0 = draw, 1 = won
       
    }
}
