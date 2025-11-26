using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// Represents a condition that checks the relationship between two characters
    /// </summary>
    [CreateAssetMenu(fileName = "New Relationship Condition", menuName = "Calliope/Branch Conditions/Relationship Condition", order = 11)]
    public class RelationshipConditionSO : BranchConditionSO
    {
        [Header("Characters")]
        [Tooltip("Role ID of the character whose opinion we're checking")]
        [SerializeField] private string fromRoleID;
        
        [Tooltip("Role ID of the character being evaluated")]
        [SerializeField] private string toRoleID;
        
        [Header("Relationship")]
        [Tooltip("The type of relationship to check")]
        [SerializeField] private RelationshipType relationshipType = RelationshipType.Opinion;
        
        [Tooltip("The threshold value to compare the relationship value against")]
        [SerializeField] [Range(0f, 100f)] private float threshold = 50f;
        
        [Tooltip("If true, relationship must be >= than the threshold; if false, the relationship must be <= than the threshold")]
        [SerializeField] private bool greaterThanOrEqual = true;

        /// <summary>
        /// Evaluates the specified trait condition using the provided set of characters and relationship data
        /// </summary>
        /// <param name="cast">A read-only dictionary that maps character IDs to their respective character objects</param>
        /// <param name="relationships">An object that provides access to relationship data between characters</param>
        /// <returns>Returns true if the trait condition is satisfied; otherwise, false</returns>
        public override bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships)
        {
            // Exit case - invalid inputs
            if (cast == null || relationships == null) return false;

            // Exit case - role IDs were not given
            if (string.IsNullOrEmpty(fromRoleID) || string.IsNullOrEmpty(toRoleID))
                return false;
            
            StringBuilder warningBuilder = new StringBuilder();
            
            // Exit case - the from-role ID cannot be found in the cast
            if (!cast.TryGetValue(fromRoleID, out ICharacter fromCharacter))
            {
                warningBuilder.Append("[RelationshipConditionSO] From-role '");
                warningBuilder.Append(fromRoleID);
                warningBuilder.Append("' not found in the cast");
                
                Debug.LogWarning(warningBuilder.ToString(), this);
                return false;
            }

            // Exit case - the to-role ID cannot be found in the cast
            if (!cast.TryGetValue(toRoleID, out ICharacter toCharacter))
            {
                warningBuilder.Append("[RelationshipConditionSO] To-role '");
                warningBuilder.Append(toRoleID);
                warningBuilder.Append("' not found in the cast");
                
                Debug.LogWarning(warningBuilder.ToString(), this);
                return false;
            }

            // Exit case - the characters do not exist
            if (fromCharacter == null || toCharacter == null) return false;
            
            // Get relationship value
            float value = relationships.GetRelationship(
                fromCharacter.ID,
                toCharacter.ID,
                relationshipType
            );
            
            // Evaluate the threshold
            if (greaterThanOrEqual) return value >= threshold;
            return value <= threshold;
        }

        /// <summary>
        /// Generates a human-readable description of the relationship condition, indicating the comparison operator
        /// and the threshold value used to evaluate relationships between characters
        /// </summary>
        /// <returns>A string representation of the relationship condition</returns>
        public override string GetDescription()
        {
            StringBuilder descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append("Relationship between characters must ");
            descriptionBuilder.Append(greaterThanOrEqual ? "be >= " : "be <= ");
            descriptionBuilder.Append(threshold);
            
            return descriptionBuilder.ToString();
        }

        private void OnValidate()
        {
            StringBuilder warningBuilder = new StringBuilder();
            
            if (string.IsNullOrEmpty(fromRoleID))
            {
                warningBuilder.Append("[RelationshipConditionSO] '");
                warningBuilder.Append(name);
                warningBuilder.Append("' has no from-role ID specified");
                
                Debug.LogWarning(warningBuilder.ToString(), this);
            }

            if (string.IsNullOrEmpty(toRoleID))
            {
                warningBuilder.Clear();
                warningBuilder.Append("[RelationshipConditionSO] '");
                warningBuilder.Append(name);
                warningBuilder.Append("' has no to-role ID specified");
                
                Debug.LogWarning(warningBuilder.ToString(), this);           
            }
        }
    }
}