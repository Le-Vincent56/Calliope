using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Validation
{
    /// <summary>
    /// Validates character definitions;
    /// Checks: ID exists, traits reference valid traits, no conflicting traits
    /// </summary>
    public class CharacterValidator : IValidator<ICharacter>
    {
        private readonly ITraitRepository _traitRepository;
        
        public CharacterValidator(ITraitRepository traitRepository)
        {
            _traitRepository = traitRepository;
        }

        /// <summary>
        /// Validates the provided character instance to ensure all fields and references are consistent and meet the required criteria
        /// </summary>
        /// <param name="character">The character instance to validate</param>
        /// <returns>A <see cref="ValidationResult"/> containing validation errors and warnings, if any</returns>
        public ValidationResult Validate(ICharacter character)
        {
            // Instantiate a new validation result
            ValidationResult result = new ValidationResult();
            
            // Create a string builder for building error messages
            StringBuilder errorBuilder = new StringBuilder();
            
            // Check ID
            if(string.IsNullOrWhiteSpace(character.ID))
                result.AddError("Character ID cannot be empty");
            
            // Check display name
            if(string.IsNullOrWhiteSpace(character.DisplayName))
                result.AddError("Character DisplayName cannot be empty");
            
            // Validate trait references
            for (int i = 0; i < character.TraitIDs.Count; i++)
            {
                string traitID = character.TraitIDs[i];
                
                // Skip if the trait exists
                if (_traitRepository.Exists(traitID)) continue;
                
                // Build the error message
                errorBuilder.Clear();
                errorBuilder.Append("Character ");
                errorBuilder.Append(character.ID);
                errorBuilder.Append(" references invalid trait ID ");
                errorBuilder.Append(traitID);
                result.AddError(errorBuilder.ToString());
            }
            
            // Track all character traits
            List<ITrait> traits = new List<ITrait>();

            for (int i = 0; i < character.TraitIDs.Count; i++)
            {
                // Get the trait
                ITrait trait = _traitRepository.GetByID(character.TraitIDs[i]);
                
                // Skip if the trait does not exist
                if (trait == null) continue;
                
                // Track the trait for conflicts
                traits.Add(trait);
            }

            StringBuilder conflictsBuilder = new StringBuilder();
            
            // Check for conflicting traits
            for (int i = 0; i < traits.Count; i++)
            {
                ITrait trait = traits[i];
                List<string> conflicts = new List<string>();

                for (int j = 0; j < trait.ConflictingTraitIDs.Count; j++)
                {
                    string conflictID = trait.ConflictingTraitIDs[j];
                    bool hasConflict = false;

                    for (int k = 0; k < character.TraitIDs.Count; k++)
                    {
                        // Check if the character has the conflicting trait
                        if (character.TraitIDs[k] == conflictID)
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                    
                    // Add the conflict to the list
                    if(hasConflict)
                        conflicts.Add(conflictID);
                }

                if (conflicts.Count > 0)
                {
                    // Build the conflicts list string
                    conflictsBuilder.Clear();
                    for (int j = 0; j < conflicts.Count; j++)
                    {
                        conflictsBuilder.Append(conflicts[j]);

                        // Skip if at the last conflict
                        if (j >= conflicts.Count - 1) continue;
                        
                        // Add commas
                        conflictsBuilder.Append(", ");
                    }
                    
                    // Build the error message
                    errorBuilder.Clear();
                    errorBuilder.Append("Trait ");
                    errorBuilder.Append(trait.ID);
                    errorBuilder.Append(" has conflicting traits: ");
                    errorBuilder.Append(conflictsBuilder.ToString());
                    result.AddError(errorBuilder.ToString());
                }
            }
            
            // Skip if the player has traits
            if (character.TraitIDs.Count != 0) return result;
            
            // If the player has no traits, warn the user
            StringBuilder warningBuilder = new StringBuilder();
            warningBuilder.Append("Character ");
            warningBuilder.Append(character.ID);
            warningBuilder.Append(" has no traits");
            result.AddWarning(warningBuilder.ToString());

            return result;
        }
    }
}