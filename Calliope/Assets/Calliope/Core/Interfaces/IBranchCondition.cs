using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A condition that must be met for a branch to be taken;
    /// Implementation examples: TraitCondition, RelationshipCondition, etc.
    /// </summary>
    public interface IBranchCondition
    {
        /// <summary>
        /// Evaluates whether this condition is true given the state of the scene
        /// </summary>
        /// <param name="cast">
        /// A read-only dictionary containing characters with their unique identifiers as keys and
        /// corresponding character objects as values; the dictionary represents the cast of the narrative system
        /// </param>
        /// <param name="relationships">
        /// An instance of the relationship provider that manages and evaluates relationships between characters
        /// </param>
        /// <returns>
        /// Returns true if the condition for taking the branch is satisfied based on the provided cast and relationships;
        /// otherwise, returns false
        /// </returns>
        bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships);

        /// <summary>
        /// Human-readable description for debugging/editor display
        /// Example: "Instigator has trait ["leader"]
        /// </summary>
        /// <returns></returns>
        string GetDescription();
    }
}