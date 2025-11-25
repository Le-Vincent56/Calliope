using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Context information for scoring a dialogue fragment; provides a speaker,
    /// target, relationships, and custom data
    /// </summary>
    public interface IScoringContext
    {
        /// <summary>
        /// The character speaking this line
        /// </summary>
        ICharacter Speaker { get; }
        
        /// <summary>
        /// The character being spoken to (can be null)
        /// </summary>
        ICharacter Target { get; }
        
        /// <summary>
        /// The relationship provider for checking opinions
        /// </summary>
        IRelationshipProvider Relationships { get; }
        
        /// <summary>
        /// Custom data for extensions (time of day, weather, etc.);
        /// allows modders to add custom scoring factors
        /// </summary>
        IReadOnlyDictionary<string, object> CustomData { get; }
    }
}