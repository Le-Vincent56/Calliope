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
        /// Evaluates whether the branch condition is satisfied based on the provided character cast,
        /// relationship data, and optional scene context
        /// </summary>
        /// <param name="cast">A read-only dictionary mapping character IDs to their respective character objects</param>
        /// <param name="relationships">An instance of a relationship provider that manages relationships between characters</param>
        /// <param name="sceneContext">Optional scene-specific context storing key-value pairs relevant to the evaluation</param>
        /// <returns>A boolean value indicating whether the condition is satisfied (true) or not (false)</returns>
        bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships,
            ISceneContext sceneContext = null);

        /// <summary>
        /// Human-readable description for debugging/editor display
        /// Example: "Instigator has trait ["leader"]
        /// </summary>
        /// <returns></returns>
        string GetDescription();
    }
}