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
        [SerializeField] private Aggregator _leftInputOverride;
        [SerializeField] private Aggregator _rightInputOverride;
        
        public float Evaluate(ISkAgent agent,WorldState worldState )
        {
            float leftValue = _leftInputOverride?.Evaluate(agent, worldState) 
                           ?? _leftInput?.Evaluate(agent, worldState) 
                           ?? 0;
            
            float rightValue = _rightInputOverride?.Evaluate(agent, worldState) 
                            ?? _rightInput?.Evaluate(agent, worldState) 
                            ?? 0;
            
            float nonClampedValue =  _function switch {
                AggregatorFunctions.Min => Mathf.Min(leftValue, rightValue),
                AggregatorFunctions.Max => Mathf.Max(leftValue, rightValue),
                AggregatorFunctions.Mult => leftValue * rightValue,
                AggregatorFunctions.Div => leftValue / rightValue,
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
        Div,
        L_Inverse,
        L_Socket,
        Add,
    }
}
