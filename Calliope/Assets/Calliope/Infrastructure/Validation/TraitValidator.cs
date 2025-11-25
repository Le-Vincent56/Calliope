using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Validation
{
    /// <summary>
    /// Validates trait definitions;
    /// Checks: ID exists, display name exists, no self-conflicts
    /// </summary>
    public class TraitValidator : IValidator<ITrait>
    {
        /// <summary>
        /// Validates the specified trait by checking if the ID and display name are provided
        /// and ensuring that the trait does not conflict with itself
        /// </summary>
        /// <param name="trait">The trait to validate; must not be null and should implement the ITrait interface</param>
        /// <returns>A <see cref="ValidationResult"/> object containing any validation errors or warnings</returns>
        public ValidationResult Validate(ITrait trait)
        {
            // Instantiate a new result
            ValidationResult result = new ValidationResult();
            
            // Check ID
            if(string.IsNullOrWhiteSpace(trait.ID))
                result.AddError("Trait ID cannot be empty");
            
            // Check display name
            if(string.IsNullOrWhiteSpace(trait.DisplayName))
                result.AddError("Trait DisplayName cannot be empty");
            
            // Check self-conflict
            bool hasSelfConflict = false;
            for (int i = 0; i < trait.ConflictingTraitIDs.Count; i++)
            {
                if (trait.ConflictingTraitIDs[i] == trait.ID)
                {
                    hasSelfConflict = true;
                    break;
                }
            }

            // Exit case  - no self-conflict
            if (!hasSelfConflict) return result;
            
            // Build the self-conflict error
            StringBuilder errorBuilder = new StringBuilder();
            errorBuilder.Append("Trait ");
            errorBuilder.Append(trait.ID);
            errorBuilder.Append(" cannot conflict with itself");
            result.AddError(errorBuilder.ToString());

            return result;
        }
    }
}