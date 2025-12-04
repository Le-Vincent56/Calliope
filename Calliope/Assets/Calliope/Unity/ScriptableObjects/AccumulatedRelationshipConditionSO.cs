using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// Represents a scriptable object defining a condition that evaluates
    /// relationships between characters, based on accumulated values and a
    /// specified threshold
    /// </summary>
    [CreateAssetMenu(fileName = "New Accumulated Relationship Condition", menuName = "Calliope/Branch Conditions/Accumulated Relationship Condition", order = 12)]
    public class AccumulatedRelationshipConditionSO : BranchConditionSO
    {
        [Header("Role Pairs")]
        [Tooltip("The pairs of roles to evaluate relationships between")]
        [SerializeField] private RolePair[] rolePairs;
        
        [Header("Aggregation")]
        [Tooltip("How to combine the relationship values")]
        [SerializeField] private AggregationType aggregationType = AggregationType.Average;
        
        [Tooltip("The type of relationship to check")]
        [SerializeField] private RelationshipType relationshipType = RelationshipType.Opinion;
        
        [Header("Comparison")]
        [Tooltip("The threshold value to compare against")]
        [SerializeField] [Range(0f, 100f)] private float threshold = 50f;
        
        [Tooltip("If true: >= threshold; if false: <= threshold")]
        [SerializeField] private bool greaterThanOrEqual = true;
        
        /// <summary>
        /// Evaluates whether the accumulated relationship conditions are satisfied
        /// based on the provided character cast, relationship data, and scene context
        /// </summary>
        /// <param name="cast">
        /// A read-only dictionary mapping role IDs to characters
        /// </param>
        /// <param name="relationships">
        /// The relationship provider used to retrieve relationship values between characters
        /// </param>
        /// <param name="sceneContext">
        /// An optional parameter representing the contextual information about the current scene
        /// </param>
        /// <returns>
        /// A boolean value indicating whether the evaluated conditions meet the specified threshold criteria
        /// </returns>
        public override bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships, ISceneContext sceneContext = null)
        {
            // Exit case - invalid parameters
            if (cast == null || relationships == null || rolePairs == null || rolePairs.Length == 0)
                return false;

            List<float> values = new List<float>();

            // Accumulate all of the directional relationship values
            for (int i = 0; i < rolePairs.Length; i++)
            {
                RolePair pair = rolePairs[i];

                // Skip if characters cannot be retrieved from the cast
                if (!cast.TryGetValue(pair.FromRoleID, out ICharacter fromCharacter)) continue;
                if (!cast.TryGetValue(pair.ToRoleID, out ICharacter toCharacter)) continue;
                if (fromCharacter == null || toCharacter == null) continue;
                
                // Get the relationship between the value and accumulate it
                float value = relationships.GetRelationship(fromCharacter.ID, toCharacter.ID, relationshipType);
                values.Add(value);
            }

            // Exit case - no values
            if (values.Count == 0) return false;

            // Aggregate the values
            float aggregatedValue = CalculateAggregate(values);
            
            return greaterThanOrEqual 
                ? aggregatedValue >= threshold 
                : aggregatedValue <= threshold;
        }

        /// <summary>
        /// Generates a descriptive string representation of the accumulated relationship condition,
        /// detailing its aggregation type, relationship type, number of role pairs, comparison operator, and threshold value
        /// </summary>
        /// <returns>
        /// A string that describes the current accumulated relationship condition
        /// </returns>
        public override string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(aggregationType);
            builder.Append(" ");
            builder.Append(relationshipType);
            builder.Append(" across ");
            builder.Append(rolePairs.Length);
            builder.Append(" role pairs ");
            builder.Append(greaterThanOrEqual ? ">= " : "<= ");
            builder.Append(threshold);
            
            return builder.ToString();
        }

        /// <summary>
        /// Calculates an aggregated value from a list of float numbers based on the specified aggregation type
        /// </summary>
        /// <param name="values">A list of float values to be aggregated; cannot be null or empty</param>
        /// <returns>
        /// A float representing the aggregated value; returns 0 if the input list is empty
        /// </returns>
        private float CalculateAggregate(List<float> values)
        {
            // Exit case - no values to aggregate
            if (values.Count == 0) return 0f;

            switch (aggregationType)
            {
                case AggregationType.Sum:
                    float sum = 0f;
                    for (int i = 0; i < values.Count; i++)
                    {
                        sum += values[i];
                    }
                    return sum;
                
                case AggregationType.Min:
                    float min = values[0];
                    for (int i = 1; i < values.Count; i++)
                    {
                        if (values[i] >= min) continue;
                        
                        min = values[i];
                    }
                    return min;
                
                case AggregationType.Max:
                    float max = values[0];
                    for (int i = 1; i < values.Count; i++)
                    {
                        if (values[i] <= max) continue;

                        max = values[i];
                    }
                    return max;
                
                case AggregationType.Average:
                default:
                    float total = 0f;
                    for (int i = 0; i < values.Count; i++)
                    {
                        total += values[i];
                    }
                    return total / values.Count;
            }
        }
    }
}
