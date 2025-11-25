using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Validation
{
    /// <summary>
    /// Validates scene templates;
    /// Checks: roles exist, beats reference valid variation sets, no unreachable beats
    /// </summary>
    public class SceneTemplateValidator : IValidator<ISceneTemplate>
    {
        private readonly IVariationSetRepository _variationSetRepository;
        
        public SceneTemplateValidator(IVariationSetRepository variationSetRepository)
        {
            _variationSetRepository = variationSetRepository;
        }

        /// <summary>
        /// Validates the specified scene template and returns a detailed validation result,
        /// including any errors or warnings found during the validation process
        /// </summary>
        /// <param name="template">The scene template to be validated</param>
        /// <returns>A <see cref="ValidationResult"/> containing validation details and any encountered issues</returns>
        public ValidationResult Validate(ISceneTemplate template)
        {
            // Instantiate a new validation result
            ValidationResult result = new ValidationResult();
            
            // Check ID
            if(string.IsNullOrWhiteSpace(template.ID))
                result.AddError("Scene template ID cannot be empty");
            
            // Check display name
            if(string.IsNullOrWhiteSpace(template.DisplayName))
                result.AddError("Scene template DisplayName cannot be empty");
            
            StringBuilder errorBuilder = new StringBuilder();
            
            // Check that there are roles
            if (template.Roles.Count == 0)
            {
                errorBuilder.Clear();
                errorBuilder.Append("Scene template ");
                errorBuilder.Append(template.ID);
                errorBuilder.Append(" has no roles defined");
                result.AddError(errorBuilder.ToString());
            }
            
            // Check that there is a starting beat
            if (string.IsNullOrWhiteSpace(template.StartingBeatID))
            {
                errorBuilder.Clear();
                errorBuilder.Append("Scene template ");
                errorBuilder.Append(template.ID);
                errorBuilder.Append(" has no starting beat defined");
                result.AddError(errorBuilder.ToString());
            } 
            // If there is, check that it is also stored as a beat for the template
            else if (!template.Beats.ContainsKey(template.StartingBeatID))
            {
                errorBuilder.Clear();
                errorBuilder.Append("Scene template ");
                errorBuilder.Append(template.ID);
                errorBuilder.Append(" references invalid starting beat ");
                errorBuilder.Append(template.StartingBeatID);
                result.AddError(errorBuilder.ToString());
            }
            
            // Collect valid role IDs
            HashSet<string> validRoleIDs = new HashSet<string>();
            for (int i = 0; i < template.Roles.Count; i++)
            {
                validRoleIDs.Add(template.Roles[i].RoleID);
            }
            
            // Validate beat graph
            HashSet<string> reachableBeats = new HashSet<string>();
            if (!string.IsNullOrWhiteSpace(template.StartingBeatID) &&
                template.Beats.ContainsKey(template.StartingBeatID))
            {
                ValidateBeatReachability(
                    template, 
                    template.StartingBeatID, 
                    reachableBeats, 
                    validRoleIDs, 
                    result
                );
            }
            
            // Warn about unreachable beats
            List<string> unreachableBeats = new List<string>();
            foreach (KeyValuePair<string, ISceneBeat> kvp in template.Beats)
            {
                // Skip over reachable beats
                if (reachableBeats.Contains(kvp.Key)) continue; 
                
                unreachableBeats.Add(kvp.Key);
            }

            if (unreachableBeats.Count > 0)
            {
                // Build the list of unreachable beats
                StringBuilder beatListBuilder = new StringBuilder();
                for (int i = 0; i < unreachableBeats.Count; i++)
                {
                    beatListBuilder.Append(unreachableBeats[i]);
                    
                    // Skip if at the last beat
                    if (i >= unreachableBeats.Count - 1) continue;
                        
                    // Add commas
                    beatListBuilder.Append(", ");
                }
                
                // Build the warning
                StringBuilder warningBuilder = new StringBuilder();
                warningBuilder.Append("Scene template ");
                warningBuilder.Append(template.ID);
                warningBuilder.Append(" has unreachable beats: ");
                warningBuilder.Append(beatListBuilder.ToString());
                result.AddWarning(warningBuilder.ToString());
            }
            
            return result;
        }

        /// <summary>
        /// Validates the reachability of beats within a scene template, ensuring that
        /// all beats are connected and accessible, and that the references to roles
        /// and variation sets are valid
        /// </summary>
        /// <param name="scene">The scene template containing beats to be validated</param>
        /// <param name="beatID">The ID of the current beat being validated</param>
        /// <param name="visited">A collection of beat IDs that have already been visited to prevent circular references</param>
        /// <param name="validRoles">A set of valid role IDs used to validate speaker and target roles within the beat</param>
        /// <param name="result">The validation result object used to log errors or issues found during the validation process</param>
        private void ValidateBeatReachability(
            ISceneTemplate scene,
            string beatID,
            HashSet<string> visited,
            HashSet<string> validRoles,
            ValidationResult result)
        {
            // Exit case - the beat has been reached
            if (visited.Contains(beatID)) return;

            StringBuilder errorBuilder = new StringBuilder();
            
            // Exit case - the beat ID is invalid
            if (!scene.Beats.TryGetValue(beatID, out ISceneBeat beat))
            {
                errorBuilder.Clear();
                errorBuilder.Append("Scene ");
                errorBuilder.Append(scene.ID);
                errorBuilder.Append(" references non-existent beat ");
                errorBuilder.Append(beatID);
                return;
            }

            // Visit the beat
            visited.Add(beatID);
            
            // Validate the speaker role
            if (!validRoles.Contains(beat.SpeakerRoleID))
            {
                // Build the error
                errorBuilder.Clear();
                errorBuilder.Append("Beat ");
                errorBuilder.Append(beatID);
                errorBuilder.Append(" references invalid speaker role ");
                errorBuilder.Append(beat.SpeakerRoleID);
                result.AddError(errorBuilder.ToString());
            }
            
            // Validate the target role
            if (!validRoles.Contains(beat.TargetRoleID))
            {
                // Build the error
                errorBuilder.Clear();
                errorBuilder.Append("Beat ");
                errorBuilder.Append(beatID);
                errorBuilder.Append(" references invalid target role ");
                errorBuilder.Append(beat.TargetRoleID);
            }
            
            // Validate the variation set references
            if (!_variationSetRepository.Exists(beat.VariationSetID))
            {
                // Build the error
                errorBuilder.Clear();
                errorBuilder.Append("Beat ");
                errorBuilder.Append(beatID);
                errorBuilder.Append(" references invalid variation set ");
                errorBuilder.Append(beat.VariationSetID);
                result.AddError(errorBuilder.ToString());
            }

            // Exit case - at the ending beat
            if (beat.IsEndBeat) return;
            
            // Recursively check branches
            for (int i = 0; i < beat.Branches.Count; i++)
            {
                ValidateBeatReachability(
                    scene, 
                    beat.Branches[i].NextBeatID, 
                    visited, 
                    validRoles, 
                    result
                );
            }
            
            // Recurse into default
            if (!string.IsNullOrEmpty(beat.DefaultNextBeatID))
            {
                ValidateBeatReachability(
                    scene,
                    beat.DefaultNextBeatID,
                    visited,
                    validRoles,
                    result
                );
            }
        }
    }
}