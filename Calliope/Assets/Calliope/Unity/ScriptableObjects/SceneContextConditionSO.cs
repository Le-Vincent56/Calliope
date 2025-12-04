using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// Provides a condition logic for branching decisions based on scene context data
    /// </summary>
    [CreateAssetMenu(fileName = "New Scene Context Condition", menuName = "Calliope/Branch Condition/Scene Context Condition", order = 13)]
    public class SceneContextConditionSO : BranchConditionSO
    {
        [Header("Context Key")]
        [Tooltip("The key to look up in the scene context")]
        [SerializeField] private string key;

        [Header("Comparison")]
        [Tooltip("How to compare the value")]
        [SerializeField] private ContextValueComparison comparison = ContextValueComparison.Exists;

        [Tooltip("Target value for comparison (used by Equals, GreaterThan, Contains, etc.)")]
        [SerializeField] private string targetValue;
        
        /// <summary>
        /// Evaluates the specified scene context condition by comparing the key's value
        /// in the provided scene context with the defined comparison operator and target value
        /// </summary>
        /// <param name="cast">A read-only dictionary of characters keyed by their identifiers, which may be used for evaluation</param>
        /// <param name="relationships">Provides relationship details between characters that may be relevant for evaluation</param>
        /// <param name="sceneContext">The current scene context containing keys and associated values to evaluate the condition against</param>
        /// <returns>
        /// A boolean indicating whether the condition matches the provided scene context
        /// </returns>
        public override bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships, ISceneContext sceneContext = null)
        {
            // Exit case - invalid parameters
            if(sceneContext == null || string.IsNullOrEmpty(key)) return false;

            switch (comparison)
            {
                case ContextValueComparison.Exists:
                    return sceneContext.HasKey(key);
                
                case ContextValueComparison.NotExists:
                    return !sceneContext.HasKey(key);
                
                case ContextValueComparison.IsTrue:
                    return sceneContext.GetValue<bool>(key, false);
                
                case ContextValueComparison.IsFalse:
                    return !sceneContext.GetValue<bool>(key, true);
                
                case ContextValueComparison.Equals:
                    return CompareEquals(sceneContext);
                
                case ContextValueComparison.NotEquals:
                    return !CompareEquals(sceneContext);
                
                case ContextValueComparison.GreaterThan:
                    return CompareNumeric(sceneContext, (a, b) => a > b);
                
                case ContextValueComparison.GreaterOrEqual:
                    return CompareNumeric(sceneContext, (a, b) => a >= b);
                
                case ContextValueComparison.LessThan:
                    return CompareNumeric(sceneContext, (a, b) => a < b);
                
                case ContextValueComparison.LessOrEqual:
                    return CompareNumeric(sceneContext, (a, b) => a <= b);
                
                case ContextValueComparison.Contains:
                    string containsValue = sceneContext.GetValue<string>(key, "");
                    return !string.IsNullOrEmpty(containsValue) && containsValue.Contains(targetValue ?? "");
                
                case ContextValueComparison.StartsWith:
                    string startsWithValue = sceneContext.GetValue<string>(key, "");
                    return !string.IsNullOrEmpty(startsWithValue) && startsWithValue.StartsWith(targetValue ?? "");
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Generates a description of the scene context condition by composing a string
        /// representation of the condition's key, comparison operator, and (if applicable)
        /// target value
        /// </summary>
        /// <returns>
        /// A string that describes the scene context condition, including the key being checked,
        /// the comparison operator being used, and the target value if it is required
        /// </returns>
        public override string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Context['");
            builder.Append(key);
            builder.Append("'] ");
            builder.Append(comparison);
            
            // Exit case - no target value required
            if (string.IsNullOrEmpty(targetValue) || !RequiresTargetValue(comparison)) return builder.ToString();
            
            builder.Append(" '");
            builder.Append(targetValue);
            builder.Append("'");

            return builder.ToString();
        }

        /// <summary>
        /// Compares a value from the scene context with a target value to determine equality,
        /// supporting both numeric and string comparisons
        /// </summary>
        /// <param name="context">
        /// The scene context containing the value to compare
        /// </param>
        /// <returns>
        /// A boolean indicating whether the value in the context equals the target value;
        /// returns false if the context value is invalid or does not match the target value
        /// </returns>
        private bool CompareEquals(ISceneContext context)
        {
            // Try numeric comparison first
            if (float.TryParse(targetValue, out float targetNum))
            {
                float contextNum = context.GetValue<float>(key, float.NaN);
                if (!float.IsNaN(contextNum)) return Math.Abs(contextNum - targetNum) < 0.0001f;
            }
            
            // Fall back to string comparison
            string contextString = context.GetValue<string>(key, null);
            return string.Equals(contextString, targetValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares a numeric value from the scene context with a target numeric value using the specified comparison logic
        /// </summary>
        /// <param name="context">
        /// The context containing the value to compare
        /// </param>
        /// <param name="comparer">
        /// A function defining the comparison logic between the context value and the target value
        /// </param>
        /// <returns>
        /// A boolean indicating whether the comparison evaluates to true;
        /// returns false if the context value or target value is not a valid number
        /// </returns>
        private bool CompareNumeric(ISceneContext context, Func<float, float, bool> comparer)
        {
            // Exit case - the target value is not a valid number
            if(!float.TryParse(targetValue, out float targetNum)) return false;
            
            float contextNum = context.GetValue<float>(key, float.NaN);
            
            // Exit case - the context value is not a valid number
            if(float.IsNaN(contextNum)) return false;
            
            return comparer(contextNum, targetNum);
        }

        /// <summary>
        /// Determines whether the specified comparison type requires a target value for evaluation
        /// </summary>
        /// <param name="comparison">
        /// The type of context value comparison to be evaluated
        /// </param>
        /// <returns>
        /// A boolean indicating whether the comparison type depends on the presence of a target value
        /// </returns>
        private bool RequiresTargetValue(ContextValueComparison comparison)
        {
            return comparison != ContextValueComparison.Exists && comparison != ContextValueComparison.NotExists
                                                               && comparison != ContextValueComparison.IsTrue && comparison != ContextValueComparison.IsFalse;
        }
    }
}