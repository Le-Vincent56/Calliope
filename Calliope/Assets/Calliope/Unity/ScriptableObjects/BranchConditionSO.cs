using System.Collections.Generic;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// Abstract base class for all types of conditions that can be applied to a beat branch
    /// </summary>
    public abstract class BranchConditionSO : ScriptableObject, IBranchCondition
    {
        /// <summary>
        /// Evaluates the condition based on the given cast of characters and their relationships
        /// </summary>
        /// <param name="cast">A read-only dictionary representing the cast of characters, keyed by character ID</param>
        /// <param name="relationships">An object that provides access to relationship data between characters</param>
        /// <returns>Returns true if the condition is met; otherwise, false</returns>
        public abstract bool Evaluate(
            IReadOnlyDictionary<string, ICharacter> cast,
            IRelationshipProvider relationships
        );

        /// <summary>
        /// Provides a description of the branch condition, detailing its purpose or requirements
        /// </summary>
        /// <returns>A string summarizing the condition this branch evaluates</returns>
        public abstract string GetDescription();
    }
}