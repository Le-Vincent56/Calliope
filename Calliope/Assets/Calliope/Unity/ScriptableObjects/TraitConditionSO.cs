using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// Represents a condition that checks whether a character has a specific trait
    /// </summary>
    [CreateAssetMenu(fileName = "New Trait Condition", menuName = "Calliope/Branch Conditions/Trait Condition", order = 10)]
    public class TraitConditionSO : BranchConditionSO
    {
        [Header("Target")]
        [Tooltip("Role ID of the character to check (e.g., 'instigator', 'objector')")]
        [SerializeField] private string roleID;
        
        [Header("Trait Check")]
        [Tooltip("The trait ID to check for")]
        [SerializeField] private string traitID;
        
        [Tooltip("If true, the character must have the trait; if false, the character must not have the trait")]
        [SerializeField] private bool mustHaveTrait = true;

        /// <summary>
        /// Evaluates the given trait condition against the provided cast of characters and their relationships
        /// </summary>
        /// <param name="cast">A read-only dictionary mapping character IDs to their respective character objects</param>
        /// <param name="relationships">An object providing access to relationship data between characters</param>
        /// <param name="sceneContext">Optional scene-specific context storing key-value pairs relevant to the evaluation</param>
        /// <returns>Returns true if the trait condition is met; otherwise, false</returns>
        public override bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships, ISceneContext sceneContext = null)
        {
            // Exit case - invalid inputs
            if (cast == null || string.IsNullOrEmpty(roleID) || string.IsNullOrEmpty(traitID))
                return false;
            
            // Exit case - the role is not found within the cast
            if (!cast.TryGetValue(roleID, out ICharacter character))
            {
                StringBuilder warningBuilder = new StringBuilder();
                warningBuilder.Append("[TraitConditionSO] Role '");
                warningBuilder.Append(roleID);
                warningBuilder.Append("' not found in the cast");
                return false;
            }

            // Exit case - the character does not exist
            if (character == null) return false;
            
            // Check the trait
            bool hasTrait = character.HasTrait(traitID);
            
            // Return based on setting
            return mustHaveTrait ? hasTrait : !hasTrait;
        }

        /// <summary>
        /// Generates a human-readable description of the condition, detailing the trait requirement
        /// and the role of the character being evaluated
        /// </summary>
        /// <returns>A string describing the condition in terms of the required trait and role</returns>
        public override string GetDescription()
        {
            StringBuilder descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append("Character must ");
            descriptionBuilder.Append(mustHaveTrait ? "have" : "not have");
            descriptionBuilder.Append(" the trait '");
            descriptionBuilder.Append(traitID);
            descriptionBuilder.Append("' in role '");
            descriptionBuilder.Append(roleID);
            descriptionBuilder.Append("'");
            
            return descriptionBuilder.ToString();
        }
    }
}