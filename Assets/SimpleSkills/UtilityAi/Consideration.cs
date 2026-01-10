using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    //https://www.youtube.com/watch?v=H6QEpc2SQiY&t=1862s
    
    public abstract class Consideration : ScriptableObject
    {
        [SerializeField] protected WorldDataKey _valueKey;
        [SerializeField] protected AnimationCurve _animationCurve;

        public abstract float Evaluate(ISkAgent currentAgent, WorldState worldState);
    }

}
