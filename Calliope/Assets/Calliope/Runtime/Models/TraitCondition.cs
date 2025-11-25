using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Branch condition based on whether a role's character has specific traits;
    /// Example: "Instigator has ["leader"] trait"
    /// </summary>
    public class TraitCondition : IBranchCondition
    {
        public string RoleID { get; set; }
        public IReadOnlyList<string> RequiredTraitIDs { get; set; }
        public bool RequireAll { get; set; }

        public TraitCondition()
        {
            RequiredTraitIDs = new List<string>();
        }

        /// <summary>
        /// Evaluates whether the condition associated with the trait-based requirements is met
        /// based on the specified character cast and relationship provider
        /// </summary>
        /// <param name="cast">
        /// A read-only dictionary containing the characters in the current narrative context,
        /// keyed by their role IDs.
        /// </param>
        /// <param name="relationships">
        /// Provides the relationship data between characters in the narrative system
        /// </param>
        /// <returns>
        /// A boolean value indicating whether the condition is met; returns true if the character
        /// associated with the specified role meets the required traits; otherwise, false
        /// </returns>
        public bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships)
        {
            // Exit case - the character is not found from the role ID
            if (!cast.TryGetValue(RoleID, out ICharacter character)) return false;

            // Check if the character has the required traits
            return RequireAll 
                ? character.HasAllTraits(RequiredTraitIDs) 
                : character.HasAnyTrait(RequiredTraitIDs);
        }

        /// <summary>
        /// Generates a human-readable description of the trait-based condition for evaluating
        /// whether a character's traits meet the specified requirements
        /// </summary>
        /// <returns>
        /// A string describing the condition, including the role identifier, whether all or any traits
        /// are required, and the list of required traits
        /// </returns>
        public string GetDescription()
        {
            StringBuilder descriptionBuilder = new StringBuilder();
            string logic = RequireAll ? "all" : "any";
            string traitsList = BuildTraitList();
            
            // Build the description
            descriptionBuilder.Append(RoleID);
            descriptionBuilder.Append(" has ");
            descriptionBuilder.Append(logic);
            descriptionBuilder.Append(" of the following traits: ");
            descriptionBuilder.Append(traitsList);
            
            return descriptionBuilder.ToString();
        }

        /// <summary>
        /// Constructs a comma-separated string of trait identifiers from the list of required traits
        /// </summary>
        /// <returns>
        /// A string containing the required trait identifiers, separated by commas
        /// </returns>
        private string BuildTraitList()
        {
            StringBuilder traitListBuilder = new StringBuilder();

            // Build the trait list
            for (int i = 0; i < RequiredTraitIDs.Count; i++)
            {
                traitListBuilder.Append(RequiredTraitIDs[i]);
                
                // Skip if at the last trait
                if(i >= RequiredTraitIDs.Count - 1) continue;

                // Add commas
                traitListBuilder.Append(", ");
            }
            
            return traitListBuilder.ToString();
        }
    }
}