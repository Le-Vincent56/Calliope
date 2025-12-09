using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Interface for dialogue line building
    /// </summary>
    public interface IDialogueLineBuilder
    {
        string BuildLine(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target,
            bool applyRelationshipModifiers = true
        );

        (string text, IScoringResult result) BuildLineWithDetails(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target, 
            bool applyRelationshipModifiers = true
        );

        void SetVariable(string key, string value);
        void ClearVariable(string key);
    }
}