using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Bundles all context needed for scoring a dialogue fragment;
    /// this is immutable, create a new instance for each scoring operation
    /// </summary>
    public class ScoringContext : IScoringContext
    {
        public ICharacter Speaker { get; }
        public ICharacter Target { get; }
        public IRelationshipProvider Relationships { get; }
        public IReadOnlyDictionary<string, object> CustomData { get; }
        
        public ScoringContext(ICharacter speaker, ICharacter target, IRelationshipProvider relationships, IReadOnlyDictionary<string, object> customData)
        {
            Speaker = speaker;
            Target = target;
            Relationships = relationships;
            CustomData = customData ?? new Dictionary<string, object>();
        }
    }
}