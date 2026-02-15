
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleEndTurn", menuName = "SimpleSkills/SimpleEndTurn")]
    public class SimpleEndTurn : SimpleSkill
    {
        public override int ID { get => (int)SkillIndex.EndTurn; }
        public override Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken,  bool isMaskingCall = false)
        {
            bool canExecute = context.OriginAgent.ActionPoints <= (GameConfig.ActionPointMax - 1);
           //const bool canExecute = true; // Always true so we don't run into the problem, that we mask everything
            context.IsValidated = canExecute;
            return Task.FromResult(canExecute);
        }

        public override Task<bool> ExecuteSkill(SkillContext context, CancellationToken cancelToken)
        {
            if(!context.IsValidated)
            {
                Debug.LogWarning("Skills should be validated before executing them!");
                bool didValidate = this.CanExecute(context, cancelToken).Result;
                if(!didValidate) return Task.FromResult(false);
            }
            
            context.OriginAgent.EndTurnAfter = true;
            return Task.FromResult(true);
        }
    }
}
