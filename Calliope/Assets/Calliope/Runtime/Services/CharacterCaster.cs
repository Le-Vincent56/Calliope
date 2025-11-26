using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;
using ILogger = Calliope.Infrastructure.Logging.ILogger;
using Random = System.Random;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Casts characters to scene roles based on trait requirements and preferences;
    /// ensures each character is only cast to one role
    /// </summary>
    public class CharacterCaster
    {
        private readonly ILogger _logger;
        private readonly Random _random;

        public CharacterCaster(ILogger logger, Random random = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _random = random ?? new Random();
        }

        /// <summary>
        /// Assigns characters to specified scene roles based on compatibility with role requirements,
        /// ensuring no character is assigned to multiple roles and logging the casting process
        /// </summary>
        /// <param name="roles">
        /// A list of scene roles to be filled; each role specifies the traits and requirements
        /// that an assigned character must fulfill
        /// </param>
        /// <param name="availableCharacters">
        /// A list of characters available for assignment; each character is evaluated against
        /// the roles' requirements to find the most suitable match
        /// </param>
        /// <returns>
        /// A read-only dictionary mapping role IDs to assigned characters; returns null if
        /// no roles or no characters are provided
        /// </returns>
        public IReadOnlyDictionary<string, ICharacter> CastScene(
            IReadOnlyList<ISceneRole> roles,
            IReadOnlyList<ICharacter> availableCharacters
        )
        {
            // Exit case - there are no roles to cast
            if (roles == null || roles.Count == 0)
            {
                _logger.LogWarning("[CharacterCaster] No roles to cast");
                return null;
            }

            if (availableCharacters == null || availableCharacters.Count == 0)
            {
                _logger.LogWarning("[CharacterCaster] No characters are available for casting");
                return null;
            }

            StringBuilder debugBuilder = new StringBuilder();
            Dictionary<string, ICharacter> cast = new Dictionary<string, ICharacter>();
            
            // Cast each role in order
            for (int i = 0; i < roles.Count; i++)
            {
                // Get the role and cast a character to it
                ISceneRole role = roles[i];
                ICharacter character = CastRole(role, availableCharacters, cast);

                // Exit case - there is no character cast to the role
                if (character == null)
                {
                    // Build the error message
                    debugBuilder.Clear();
                    debugBuilder.Append("[CharacterCaster] No character could be cast to role '");
                    debugBuilder.Append(role.RoleID);
                    debugBuilder.Append("'");
                    
                    // Log the error
                    _logger.LogError(debugBuilder.ToString());
                    return null;
                }
                
                // Cast the role to the character
                cast[role.RoleID] = character;
                
                // Build the debug message
                debugBuilder.Clear();
                debugBuilder.Append("[CharacterCaster] Cast '");
                debugBuilder.Append(character.DisplayName);
                debugBuilder.Append("' as '");
                debugBuilder.Append(role.RoleID);
                debugBuilder.Append("'");
                
                _logger.LogInfo(debugBuilder.ToString());
            }

            return cast;
        }

        /// <summary>
        /// Assigns a character to a specified scene role from a list of available characters;
        /// the assignment process considers role requirements, evaluates potential candidates,
        /// and respects restrictions such as avoiding characters already cast in other roles
        /// </summary>
        /// <param name="role">
        /// The scene role to which a character will be assigned; the role specifies
        /// the required and forbidden traits for eligible characters
        /// </param>
        /// <param name="availableCharacters">
        /// The list of characters available for assignment; each character is evaluated
        /// to determine compatibility with the role requirements
        /// </param>
        /// <param name="existingCast">
        /// A dictionary of roles already assigned to characters; ensures that no character
        /// is duplicated across multiple roles
        /// </param>
        /// <returns>
        /// The character assigned to the specified role, or null if no suitable candidate is found
        /// </returns>
        private ICharacter CastRole(
            ISceneRole role,
            IReadOnlyList<ICharacter> availableCharacters,
            IReadOnlyDictionary<string, ICharacter> existingCast
        )
        {
            // Find valid candidates
            List<ICharacter> candidates = FindValidCandidates(role, availableCharacters, existingCast);

            // Exit case - no valid candidates found
            if (candidates.Count == 0)
            {
                // Build the error message
                StringBuilder debugBuilder = new StringBuilder();
                debugBuilder.Append("[CharacterCaster] No valid candidates found for role '");
                debugBuilder.Append(role.RoleID);
                debugBuilder.Append("'");
                
                _logger.LogWarning(debugBuilder.ToString());
            }
            
            // Score candidates by preference
            List<(ICharacter, float)> scored = ScoreCandidates(candidates, role);

            // Select the best candidate
            return SelectCandidate(scored);
        }

        /// <summary>
        /// Identifies and returns a list of characters that are valid candidates for a given scene role;
        /// validation includes ensuring that the character is not already cast, fulfills required traits,
        /// and does not possess forbidden traits specified by the role
        /// </summary>
        /// <param name="role">
        /// The scene role for which valid candidates need to be identified; the role specifies required
        /// and forbidden traits that candidates must satisfy.
        /// </param>
        /// <param name="availableCharacters">
        /// The list of all characters available for casting; each character is evaluated to determine
        /// if they meet the requirements of the specified role
        /// </param>
        /// <param name="existingCast">
        /// A dictionary representing the characters already cast to roles; ensures that no character
        /// is cast to multiple roles in the same scene
        /// </param>
        /// <returns>
        /// A list of characters who satisfy all the requirements of the given scene role and are not
        /// already cast to another role
        /// </returns>
        private List<ICharacter> FindValidCandidates(
            ISceneRole role,
            IReadOnlyList<ICharacter> availableCharacters,
            IReadOnlyDictionary<string, ICharacter> existingCast
        )
        {
            List<ICharacter> valid = new List<ICharacter>();

            for (int i = 0; i < availableCharacters.Count; i++)
            {
                ICharacter character = availableCharacters[i];
                
                // Check if the character is cast already
                bool alreadyCast = false;
                foreach (ICharacter castCharacter in existingCast.Values)
                {
                    // Skip if the IDs don't match
                    if (castCharacter.ID != character.ID) continue;
                    
                    // Notify already cast
                    alreadyCast = true;
                    break;
                }

                // Skip if already cast
                if (alreadyCast) continue;
                
                // Check required traits
                bool meetsRequirements = true;
                for (int j = 0; j < role.RequiredTraitIDs.Count; j++)
                {
                    // Skip if the character has the trait
                    if (character.HasTrait(role.RequiredTraitIDs[j])) continue;
                    
                    // Notify requirement failure
                    meetsRequirements = false;
                    break;
                }
                
                // Skip if the character does not meet the trait requirements
                if (!meetsRequirements) continue;
                
                // Check forbidden traits
                bool hasForbidden = false;
                for (int j = 0; j < role.ForbiddenTraitIDs.Count; j++)
                {
                    // Skip if the character does not have a forbidden trait
                    if (!character.HasTrait(role.ForbiddenTraitIDs[j])) continue;
                    
                    // Notify forbidden trait
                    hasForbidden = true;
                    break;
                }

                // Skip if the character has a forbidden trait
                if (hasForbidden) continue;
                
                valid.Add(character);
            }

            return valid;
        }

        /// <summary>
        /// Scores a list of candidate characters based on their compatibility with a specified scene role;
        /// each candidate is evaluated by the role's preferred traits, with points added per matching trait
        /// </summary>
        /// <param name="candidates">
        /// A list of characters to evaluate; each character will be scored based on their traits
        /// </param>
        /// <param name="role">
        /// The scene role that defines the preferred traits against which candidates are assessed
        /// </param>
        /// <returns>
        /// A list of tuples where each tuple consists of a candidate character and their corresponding
        /// score, in the same order as the input list
        /// </returns>
        private List<(ICharacter, float)> ScoreCandidates(List<ICharacter> candidates, ISceneRole role)
        {
            List<(ICharacter, float)> scored = new List<(ICharacter, float)>(candidates.Count);

            // Score all of the characters based on traits
            for (int i = 0; i < candidates.Count; i++)
            {
                ICharacter character = candidates[i];
                float score = 0f;
                
                // Add points for each preferred trait
                for (int j = 0; j < role.PreferredTraits.Count; j++)
                {
                    // Skip if the character does not have the trait
                    if (!character.HasTrait(role.PreferredTraits[j].TraitID)) continue;
                    
                    // Add the weight of the trait
                    score += role.PreferredTraits[j].Weight;
                }
                
                scored.Add((character, score));
            }

            return scored;
        }

        /// <summary>
        /// Selects the best candidate from a scored list of characters;
        /// characters are evaluated based on their assigned scores, and in the case of
        /// a tie, one is selected randomly
        /// </summary>
        /// <param name="scored">
        /// A list of tuples where each tuple contains a character and their corresponding score
        /// </param>
        /// <returns>
        /// The character with the highest score from the list; if multiple characters
        /// share the highest score, one is selected randomly; returns null if the list is empty
        /// </returns>
        private ICharacter SelectCandidate(List<(ICharacter, float)> scored)
        {
            // Exit case - no candidates found
            if (scored.Count == 0) return null;
            
            // Find the max score
            float maxScore = float.MinValue;
            for (int i = 0; i < scored.Count; i++)
            {
                // Skip if the score is less than or equal to the current max
                if (scored[i].Item2 <= maxScore) continue;
                
                // Update the max score
                maxScore = scored[i].Item2;
            }
            
            // Collect all candidates with the max score
            List<ICharacter> best = new List<ICharacter>();
            for (int i = 0; i < scored.Count; i++)
            {
                // Skip if the score is not equal to the max
                if (!Mathf.Approximately(scored[i].Item2, maxScore)) continue;
                
                best.Add(scored[i].Item1);
            }
            
            // Random selection from tied candidates
            int index = _random.Next(best.Count);
            return best[index];
        }
    }
}