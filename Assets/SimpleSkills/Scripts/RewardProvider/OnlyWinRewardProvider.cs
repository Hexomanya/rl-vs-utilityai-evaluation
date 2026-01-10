
namespace SimpleSkills.Scripts.RewardProvider
{
    public class OnlyWinRewardProvider : RewardProvider
    {
        private const int _winReward = 1;
        
        public OnlyWinRewardProvider(MlSkAgent agent, SkGameplayManager gameplayManager) : base(agent, gameplayManager) { }
        
        public override void OnDidWin() => this.AddReward(_winReward);
        public override void OnDidLoose() => this.AddReward(-_winReward);
        public override void OnDidDraw() {}
        public override void OnDidDealDamage(ISkAgent target, int damage) {}
        public override void OnDidReceiveDamage(ISkAgent origin, int damage) {}
        public override void OnDidMove(float distance) {}
        public override void OnDidKill(ISkAgent killedAgent) { }
        public override void OnDidDie() { }
    }
}
