
namespace SimpleSkills.Scripts.RewardProvider
{
    public class DamageWinRewardProvider : RewardProvider
    {
        private const int _winReward = 10;
        private const float _damageTakenMultiplier = 1f;

        public DamageWinRewardProvider(MlSkAgent agent, SkGameplayManager gameplayManager) : base(agent, gameplayManager) { }

        public override void OnDidWin() => this.AddReward(_winReward);
        public override void OnDidLoose() => this.AddReward(-_winReward);
        public override void OnDidDraw() { }
        public override void OnDidDealDamage(ISkAgent target, int damage) => this.AddReward(damage);
        public override void OnDidReceiveDamage(ISkAgent origin, int damage) => this.AddReward(damage * _damageTakenMultiplier * -1);
        public override void OnDidMove(float distance) {}
        public override void OnDidKill(ISkAgent killedAgent) { }
        public override void OnDidDie() { }
    }
}
