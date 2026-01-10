using System;
using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [CreateAssetMenu(fileName = "Aggregator", menuName = "UtilityAi/Aggregator")]
    public class Aggregator : ScriptableObject
    {
        [SerializeField] private AggregatorFunctions _function;
        [SerializeField] private Consideration _leftInput;
        [SerializeField] private Consideration _rightInput;
        
        public float Evaluate(ISkAgent agent,WorldState worldState )
        {
            float leftValue = _leftInput?.Evaluate(agent, worldState) ?? 0;
            float rightValue = _rightInput?.Evaluate(agent, worldState) ?? 0;
            
            float nonClampedValue =  _function switch {
                AggregatorFunctions.Min => Mathf.Min(leftValue, rightValue),
                AggregatorFunctions.Max => Mathf.Max(leftValue, rightValue),
                AggregatorFunctions.Mult => leftValue * rightValue,
                AggregatorFunctions.L_Inverse => 1 - leftValue,
                AggregatorFunctions.L_Socket => leftValue,
                AggregatorFunctions.Add => leftValue + rightValue,
                _ => 0,
            };
            
            //Debug.Log($"Non value: {nonClampedValue}");

            return Mathf.Clamp01(nonClampedValue);
        }
    }

    public enum AggregatorFunctions
    {
        Min,
        Max,
        Mult,
        L_Inverse,
        L_Socket,
        Add,
    }
}
