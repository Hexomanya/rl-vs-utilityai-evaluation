using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleSkills.Implementations
{
    [CreateAssetMenu(fileName = "SimpleDoNothing", menuName = "SimpleSkills/SimpleDoNothing")]
    public class SimpleDoNothing : SimpleSkill
    {
        public override int ID { get => -1; }
        public override Task<bool> CanExecute(SkillContext context, CancellationToken cancelToken,  bool isMaskingCall = false)
        {
            const bool canExecute = true;
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
            
            return Task.FromResult(true);
        }
    }
}
