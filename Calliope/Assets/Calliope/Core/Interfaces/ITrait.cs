using System.Collections.Generic;
using Calliope.Core.Enums;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Represents a character trait that influences dialogue selection
    /// Examples: Aggressive, Diplomatic, Brave, Timid
    /// </summary>
    public interface ITrait
    {
        /// <summary>
        /// Unique identifier ("aggressive", "diplomatic")
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Display name shown in UI ("Aggressive", "Diplomatic")
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// A description of what this trait means
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// The category this trait belongs to
        /// </summary>
        TraitCategory Category { get; }
        
        /// <summary>
        /// Traits that cannot coexist with this one
        /// </summary>
        IReadOnlyList<string> ConflictingTraitIDs { get; }
    }
}