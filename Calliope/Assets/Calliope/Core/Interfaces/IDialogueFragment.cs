using System.Collections.Generic;
using Calliope.Core.ValueObjects;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A single dialogue variation that can be selected;
    /// contains text, trait affinities, and selection constraints
    /// </summary>
    public interface IDialogueFragment
    {
        /// <summary>
        /// Unique identifier (e.g., "proposal_aggressive_1"
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Raw text with variables;
        /// Example: "{speaker.name} glares at {target.name}. \"We're taking the mountains.\""
        /// </summary>
        string Text { get; }
        
        /// <summary>
        /// The trait affinities affecting selection probability;
        /// Example: aggressive +1.0 makes aggressive characters more likely to use this fragment
        /// </summary>
        IReadOnlyList<TraitAffinity> TraitAffinities { get; }
        
        /// <summary>
        /// The relationship modifiers affecting selection probability;
        /// Example: Opinion >= 70 multiplies score by 1.5x
        /// </summary>
        IReadOnlyList<RelationshipModifier> RelationshipModifiers { get; }
        
        /// <summary>
        /// The traits that must be present for this fragment to be valid;
        /// Example: ["leader"] means only characters with the "leader" trait can use this
        /// </summary>
        IReadOnlyList<string> RequiredTraitIDs { get; }
        
        /// <summary>
        /// The traits that prevent this fragment from being valid;
        /// Example: ["timid"] means timid characters cannot use this
        /// </summary>
        IReadOnlyList<string> ForbiddenTraitIDs { get; }
        
        /// <summary>
        /// Optional tags for additional filtering (e.g., "angry", "sarcastic")
        /// </summary>
        IReadOnlyList<string> Tags { get; }
        
        /// <summary>
        /// Context modifiers applied when this fragment is selected;
        /// enables data-driven scene state tracking
        /// </summary>
        IReadOnlyList<ContextModifier> ContextModifiers { get; }
    }
}