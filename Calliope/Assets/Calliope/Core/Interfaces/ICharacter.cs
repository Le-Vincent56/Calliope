using System.Collections.Generic;
using Calliope.Core.ValueObjects;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Represents a character in the narrative system;
    /// characters have traits that influence dialogue selection
    /// </summary>
    public interface ICharacter
    {
        /// <summary>
        /// Unique identifier ("aldric", "sera", "finn")
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Display name shown in UI ("Aldric", "Sera", "Finn))
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Pronounce set for the character, used for text variable substitution
        /// </summary>
        PronounSet Pronouns { get; }
        
        /// <summary>
        /// IDs of all the traits this character possesses
        /// </summary>
        IReadOnlyList<string> TraitIDs { get; }
        
        /// <summary>
        /// The faction this character belongs to (optional)
        /// </summary>
        string FactionID { get; }

        /// <summary>
        /// Determines if the character possesses a specific trait based on the provided trait ID
        /// </summary>
        /// <param name="traitID">The unique identifier of the trait to check for in the character's trait list</param>
        /// <returns>True if the character has the specified trait; otherwise, false</returns>
        bool HasTrait(string traitID);

        /// <summary>
        /// Determines if the character possesses any of the specified traits based on the provided list of trait IDs
        /// </summary>
        /// <param name="traitIDs">A collection of unique trait identifiers to check against the character's trait list</param>
        /// <returns>True if the character has at least one of the specified traits; otherwise, false</returns>
        bool HasAnyTrait(IEnumerable<string> traitIDs);

        /// <summary>
        /// Determines if the character possesses all the specified traits based on the provided list of trait IDs
        /// </summary>
        /// <param name="traitIDs">A collection of unique trait identifiers to check against the character's trait list</param>
        /// <returns>True if the character has all of the specified traits; otherwise, false</returns>
        bool HasAllTraits(IEnumerable<string> traitIDs);
    }
}