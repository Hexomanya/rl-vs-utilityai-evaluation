using System;
using System.Collections.Generic;
using ArtificialEnemy;
using UnityEngine;

namespace _General
{
    public enum GameplayState
    {
        Running,
        Paused,
        Stopped,
        WinEndState,
    }

    public interface IGameplayManager
    {
        public void OnStart(ScoreKeeper scoreKeeper, RunConfig config);
        public Dictionary<string, int> OnStop();
        public bool OnActionTook(IAgent agent, int actionIndex);
        public void OnTurnDone(IAgent agent, int stayAtIndex = -1);
        public GameState GetGameState(int playerIndex);
        public bool OnPositionUpdate(IAgent movedAgent, Vector2Int newPosition, Vector2Int oldPosition);
        public void RemoveFromGame(IAgent mlSkAgent);
        public Vector2 GetMultipleGameOffset();
        public event Action<string> OnGameEnded;
    }
}
