using Calliope.Core.Enums;

namespace Calliope.Infrastructure.Events
{
    /// <summary>
    /// Fired when a relationship value changes between two characters;
    /// useful for achievements, analytics, and debugging
    /// </summary>
    public class RelationshipChangedEvent : CalliopeEventBase
    {
        /// <summary>
        /// The character whose opinion has changed
        /// </summary>
        public string FromCharacterID { get; }
        
        /// <summary>
        /// The character the opinion is about
        /// </summary>
        public string ToCharacterID { get; }
        
        /// <summary>
        /// The type of relationship that changed
        /// </summary>
        public RelationshipType RelationshipType { get; }
        
        /// <summary>
        /// The previous value before the change
        /// </summary>
        public float OldValue { get; }
        
        /// <summary>
        /// The new value after the change
        /// </summary>
        public float NewValue { get; }
        
        /// <summary>
        /// The change amount
        /// </summary>
        public float Delta => NewValue - OldValue;

        public RelationshipChangedEvent(
            string fromCharacterID, 
            string toCharacterID, 
            RelationshipType relationshipType,
            float oldValue, 
            float newValue)
        {
            FromCharacterID = fromCharacterID;
            ToCharacterID = toCharacterID;
            RelationshipType = relationshipType;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}