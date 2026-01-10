using _General;
using Unity.MLAgents;
using UnityEngine;

namespace SimpleSkills.Scripts.RewardProvider
{
    public abstract class RewardProvider
    {
        protected readonly SkGameplayManager _gameplayManager;
        protected readonly MlSkAgent _agent;
        
        protected RewardProvider(MlSkAgent agent, SkGameplayManager gameplayManager)
        {
            _agent = agent;
            _gameplayManager = gameplayManager;
        }
        
        protected void AddReward(float increment)
        {
            _agent.AddReward(increment);
        }
        
        public abstract void OnDidWin();
        public abstract void OnDidLoose();
        public abstract void OnDidDraw();
        public abstract void OnDidDealDamage(ISkAgent target, int damage);
        public abstract void OnDidReceiveDamage(ISkAgent origin, int damage);
        public abstract void OnDidMove(float distance);
        public abstract void OnDidKill(ISkAgent killedAgent);
        public abstract void OnDidDie();
    }
}
