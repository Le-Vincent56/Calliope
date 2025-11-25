using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Validation
{
    /// <summary>
    /// Validates dialogue fragments;
    /// Checks: ID exists, text exists, trait references exist, no trait contradictions
    /// </summary>
    public class DialogueFragmentValidator : IValidator<IDialogueFragment>
    {
        private readonly ITraitRepository _traitRepository;

        public DialogueFragmentValidator(ITraitRepository traitRepository)
        {
            _traitRepository = traitRepository;
        }

        /// <summary>
        /// Validates a dialogue fragment to ensure it conforms to required rules and constraints
        /// </summary>
        /// <param name="fragment">The <see cref="IDialogueFragment"/> instance to validate</param>
        /// <returns>A <see cref="ValidationResult"/> containing validation outcomes, including any errors or warnings</returns>
        public ValidationResult Validate(IDialogueFragment fragment)
        {
            // Instantiate a new validation result
            ValidationResult result = new ValidationResult();
            
            // Check the ID
            if(string.IsNullOrWhiteSpace(fragment.ID))
                result.AddError("Dialogue fragment ID cannot be empty");
            
            // Check text
            if(string.IsNullOrWhiteSpace(fragment.Text))
                result.AddError("Dialogue fragment text cannot be empty");
            
            StringBuilder errorBuilder = new StringBuilder();
            
            // Validate trait affinity references
            for (int i = 0; i < fragment.TraitAffinities.Count; i++)
            {
                string traitID = fragment.TraitAffinities[i].TraitID;

                // Skip if the trait exists
                if (_traitRepository.Exists(traitID)) continue;
                
                // Build the error
                errorBuilder.Clear();
                errorBuilder.Append("Dialogue fragment ");
                errorBuilder.Append(fragment.ID);
                errorBuilder.Append(" references non-existent trait ");
                errorBuilder.Append(traitID);
                errorBuilder.Append(" in its trait affinities");
                result.AddError(errorBuilder.ToString());
            }
            
            // Validate required trait references
            for (int i = 0; i < fragment.RequiredTraitIDs.Count; i++)
            {
                string traitID = fragment.RequiredTraitIDs[i];

                // Skip if the trait exists
                if (_traitRepository.Exists(traitID)) continue;
                
                // Build the error
                errorBuilder.Clear();
                errorBuilder.Append("Dialogue fragment ");
                errorBuilder.Append(fragment.ID);
                errorBuilder.Append(" references non-existent trait ");
                errorBuilder.Append(traitID);
                errorBuilder.Append(" in its required traits");
                result.AddError(errorBuilder.ToString());
            }
            
            // Validate forbidden trait references
            for (int i = 0; i < fragment.ForbiddenTraitIDs.Count; i++)
            {
                string traitID = fragment.ForbiddenTraitIDs[i];

                // Skip if the trait exists
                if (_traitRepository.Exists(traitID)) continue;
                
                // Build error
                errorBuilder.Clear();
                errorBuilder.Append("Dialogue fragment ");
                errorBuilder.Append(fragment.ID);
                errorBuilder.Append(" references non-existent trait ");
                errorBuilder.Append(traitID);
                errorBuilder.Append(" in its forbidden traits");
                result.AddError(errorBuilder.ToString());
            }
            
            // Check for contradictions (trait is required and forbidden)
            List<string> contradictions = new List<string>();
            for (int i = 0; i < fragment.RequiredTraitIDs.Count; i++)
            {
                string requiredID = fragment.RequiredTraitIDs[i];
                for (int j = 0; j < fragment.ForbiddenTraitIDs.Count; j++)
                {
                    if (fragment.ForbiddenTraitIDs[j] == requiredID)
                    {
                        contradictions.Add(requiredID);
                        break;
                    }
                }
            }

            if (contradictions.Count > 0)
            {
                // Build the list of contradictions
                StringBuilder contradictionsList = new StringBuilder();
                for (int i = 0; i < contradictions.Count; i++)
                {
                    contradictionsList.Append(contradictions[i]);
                    
                    // Skip if at the last contradiction
                    if(i >= contradictions.Count - 1) continue;

                    // Add commas
                    contradictionsList.Append(", ");
                }
                
                // Build the error
                errorBuilder.Clear();
                errorBuilder.Append("Dialogue fragment ");
                errorBuilder.Append(fragment.ID);
                errorBuilder.Append(" has contradictory required and forbidden traits: ");
                errorBuilder.Append(contradictionsList.ToString());
                result.AddError(errorBuilder.ToString());
            }
            
            // Warn about unclosed braces for substitution
            StringBuilder warningBuilder = new StringBuilder();
            if (fragment.Text.Contains('{') && !fragment.Text.Contains('}'))
            {
                warningBuilder.Clear();
                warningBuilder.Append("Dialogue fragment ");
                warningBuilder.Append(fragment.ID);
                warningBuilder.Append(" has unclosed variable brace");
                result.AddWarning(warningBuilder.ToString());
            }
            
            if (fragment.Text.Contains('}') && !fragment.Text.Contains('{'))
            {
                warningBuilder.Clear();
                warningBuilder.Append("Dialogue fragment ");
                warningBuilder.Append(fragment.ID);
                warningBuilder.Append(" has unmatched closing brace");
            }
            
            return result;
        }
    }
}