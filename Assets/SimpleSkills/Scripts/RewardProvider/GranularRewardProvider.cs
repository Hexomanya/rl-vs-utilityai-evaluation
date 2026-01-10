using UnityEngine;

namespace SimpleSkills.Scripts.RewardProvider
{
    public class GranularRewardProvider : RewardProvider
    {

        private const float _winReward = 1;
        private const float _damageMultiplier = 0.05f;
        private const float _friendlyFireMultiplier = -0.1f;
        private const float _friendlyKillPenalty = -2f;
        
        public GranularRewardProvider(MlSkAgent agent, SkGameplayManager gameplayManager) : base(agent, gameplayManager) { }
        
        public override void OnDidWin() {
            if(!_agent.UsesRewardShaping)
            {
                this.AddReward(_winReward);
                return;
            }

            float progressRatio = _gameplayManager.CurrentRound / (float)_gameplayManager.RunConfig.MaxRounds;
            if(progressRatio is < 0 or > 1) Debug.LogWarning("Progress ratio is outside of bounds!");
            
            float earlyWinBonus = Mathf.Pow(1 - progressRatio, 3) * 0.05f;
            float adjustedReward = _winReward + earlyWinBonus;
            this.AddReward(adjustedReward);
        }
        public override void OnDidLoose() => this.AddReward(-_winReward);
        public override void OnDidDraw() => this.AddReward(-_winReward);

        public override void OnDidDealDamage(ISkAgent target, int damage)
        {
            if(!this.IsSameTeam(target)) this.AddReward(damage * _damageMultiplier);
            else this.AddReward(damage * _friendlyFireMultiplier);
        }
        public override void OnDidReceiveDamage(ISkAgent origin, int damage) => this.AddReward(-damage * _damageMultiplier);
        public override void OnDidMove(float distance){} // => this.AddReward(0.01f); 

        public override void OnDidKill(ISkAgent killedAgent)
        {
            if(this.IsSameTeam(killedAgent)) this.AddReward(_friendlyKillPenalty);
        }
        
        public override void OnDidDie() { }

        private bool IsSameTeam(ISkAgent skAgent)
        {
            return _agent.FactionIndex == skAgent.FactionIndex;
        }
    }
}
