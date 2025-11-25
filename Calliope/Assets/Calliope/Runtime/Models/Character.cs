using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a character within the narrative system; characters have traits and pronouns
    /// that contribute to their identity, impacting various aspects such as dialogue selection
    /// </summary>
    public class Character : ICharacter
    {
        public string ID { get; set; }
        public string DisplayName { get; set; }
        public PronounSet Pronouns { get; }
        public IReadOnlyList<string> TraitIDs { get; set; }

        public Character()
        {
            Pronouns = PronounSet.TheyThem;
            TraitIDs = new List<string>();
        }

        /// <summary>
        /// Checks if the character possesses the specified trait
        /// </summary>
        /// <param name="traitID">The ID of the trait to check for</param>
        /// <returns>True if the character has the specified trait; otherwise, false</returns>
        public bool HasTrait(string traitID)
        {
            for (int i = 0; i < TraitIDs.Count; i++)
            {
                // Exit case - the trait was found
                if (TraitIDs[i] == traitID) return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the character possesses at least one of the specified traits
        /// </summary>
        /// <param name="traitIDs">A collection of trait IDs to check against the character's traits</param>
        /// <returns>True if the character has at least one of the specified traits; otherwise, false</returns>
        public bool HasAnyTrait(IEnumerable<string> traitIDs)
        {
            foreach (string traitID in traitIDs)
            {
                if(HasTrait(traitID)) return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the character possesses all of the specified traits
        /// </summary>
        /// <param name="traitIDs">A collection of trait IDs to check against the character's traits</param>
        /// <returns>True if the character has all the specified traits; otherwise, false</returns>
        public bool HasAllTraits(IEnumerable<string> traitIDs)
        {
            foreach (string traitID in traitIDs)
            {
                if (!HasTrait(traitID))
                    return false;
            }

            return true;
        }
    }
}