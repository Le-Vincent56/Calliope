using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A collection of dialogue fragments, one of which will be selected
    /// Example: "instigator_proposal" contains 3 variations (aggressive, brave, neutral)
    /// </summary>
    public interface IVariationSet
    {
        /// <summary>
        /// Unique identifier ("instigator_proposal")
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// The display name for the editor ("Instigator Proposal")
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// All possible variations for this dialogue moment
        /// </summary>
        IReadOnlyList<IDialogueFragment> Variations { get; }
    }
}