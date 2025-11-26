using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using Calliope.Infrastructure.Logging;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Manages relationship values between characters;
    /// relationships are directional: A's opinion of B is separate from
    /// B's opinion of A; thread-safe for reading and writing relationship values
    /// </summary>
    public class RelationshipProvider : IRelationshipProvider
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<RelationshipType, float>>> _relationships;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private const float DefaultRelationshipValue = 50f;

        public RelationshipProvider(IEventBus eventBus, ILogger logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _relationships = new Dictionary<string, Dictionary<string, Dictionary<RelationshipType, float>>>();
        }

        /// <summary>
        /// Retrieves the relationship score of a specific type from one character to another.
        /// </summary>
        /// <param name="fromCharacterID">The unique identifier of the character initiating the relationship query</param>
        /// <param name="toCharacterID">The unique identifier of the target character whose relationship score is being queried</param>
        /// <param name="type">The type of relationship to evaluate, such as an opinion or affinity</param>
        /// <returns>
        /// The float value representing the relationship score of the specified type;
        /// returns a default value if no relationship or type is defined
        /// </returns>
        public float GetRelationship(string fromCharacterID, string toCharacterID, RelationshipType type)
        {
            // Lock for thread-safety
            lock (_lock)
            {
                // Exit case - fromCharacter has no relationships
                if(!_relationships.TryGetValue(fromCharacterID, out Dictionary<string, Dictionary<RelationshipType, float>> fromDict))
                    return DefaultRelationshipValue;
                
                // Check if fromCharacter has relationships with toCharacter
                if(!fromDict.TryGetValue(toCharacterID, out Dictionary<RelationshipType, float> toDict))
                    return DefaultRelationshipValue;
                
                // Check the specific relationship type
                return toDict.GetValueOrDefault(type, DefaultRelationshipValue);
            }
        }

        /// <summary>
        /// Sets the relationship score of a specific type between two characters to a specified value, ensuring the value stays within a valid range
        /// </summary>
        /// <param name="fromCharacterID">The unique identifier of the character initiating the relationship change</param>
        /// <param name="toCharacterID">The unique identifier of the target character whose relationship score is being modified</param>
        /// <param name="type">The type of relationship being set, such as opinion or affinity</param>
        /// <param name="value">The new value of the relationship score to be applied, which will be clamped to a valid range</param>
        public void SetRelationship(string fromCharacterID, string toCharacterID, RelationshipType type, float value)
        {
            // Clamp the value to a valid range
            float clampedValue = Math.Clamp(value, 0f, 100f);

            float oldValue;
            
            // Lock for thread-safety
            lock (_lock)
            {
                // Get the old value for the event
                oldValue = GetRelationship(fromCharacterID, toCharacterID, type);
                
                // Ensure nested dictionaries exist
                if(!_relationships.ContainsKey(fromCharacterID))
                    _relationships[fromCharacterID] = new Dictionary<string, Dictionary<RelationshipType, float>>();
                
                Dictionary<string, Dictionary<RelationshipType, float>> fromDict = _relationships[fromCharacterID];
                
                if(!fromDict.ContainsKey(toCharacterID))
                    fromDict[toCharacterID] = new Dictionary<RelationshipType, float>();

                Dictionary<RelationshipType, float> toDict = fromDict[toCharacterID];
                
                // Set the new value
                toDict[type] = clampedValue;
            }

            // Build the log message
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("[RelationshipProvider] '");
            debugBuilder.Append(fromCharacterID);
            debugBuilder.Append("' to '");
            debugBuilder.Append(toCharacterID);
            debugBuilder.Append(" (");
            debugBuilder.Append(type);
            debugBuilder.Append("): ");
            debugBuilder.Append(oldValue.ToString("F1"));
            debugBuilder.Append(" to ");
            debugBuilder.Append(clampedValue.ToString("F1"));
            
            // Log the change
            _logger.LogInfo(debugBuilder.ToString());
            
            // Publish the event
            _eventBus.Publish(new RelationshipChangedEvent(
                fromCharacterID, 
                toCharacterID, 
                type, 
                oldValue, 
                clampedValue
            ));
        }

        /// <summary>
        /// Modifies the relationship score of a specific type between two characters by applying a delta value
        /// </summary>
        /// <param name="fromCharacterID">The unique identifier of the character whose relationship score is being adjusted</param>
        /// <param name="toCharacterID">The unique identifier of the target character whose relationship score is affected</param>
        /// <param name="type">The type of relationship being adjusted, such as an opinion</param>
        /// <param name="delta">The value to add to or subtract from the current relationship score</param>
        public void ModifyRelationship(string fromCharacterID, string toCharacterID, RelationshipType type, float delta)
        {
            // Get the current relationship value
            float currentValue = GetRelationship(fromCharacterID, toCharacterID, type);
            
            // Calculate the new value
            float newValue = currentValue + delta;
            
            // Set the new value
            SetRelationship(fromCharacterID, toCharacterID, type, newValue);
        }

        /// <summary>
        /// Retrieves all relationships associated with a specific character, including the relationship
        /// details with every other character and their respective relationship types and scores
        /// </summary>
        /// <param name="characterID">The unique identifier of the character whose relationships are being retrieved</param>
        /// <returns>
        /// A dictionary where the key represents the target character ID, and the value contains
        /// another dictionary mapping relationship types to their respective scores; returns an
        /// empty dictionary if the character has no relationships
        /// </returns>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<RelationshipType, float>> GetAllRelationships(string characterID)
        {
            // Lock for thread-safety
            lock (_lock)
            {
                // Exit case - the character has no relationships
                if (!_relationships.TryGetValue(characterID, out Dictionary<string, Dictionary<RelationshipType, float>> fromDict))
                    return new Dictionary<string, IReadOnlyDictionary<RelationshipType, float>>();
                
                // Deep copy to avoid external modification
                Dictionary<string, IReadOnlyDictionary<RelationshipType, float>> result = new Dictionary<string, IReadOnlyDictionary<RelationshipType, float>>();

                foreach (KeyValuePair<string, Dictionary<RelationshipType, float>> kvp in fromDict)
                {
                    string targetID = kvp.Key;
                    Dictionary<RelationshipType, float> typeDict = kvp.Value;
                    
                    // Copy inner directory
                    Dictionary<RelationshipType, float> innerCopy = new Dictionary<RelationshipType, float>();
                    foreach (KeyValuePair<RelationshipType, float> innerKvp in typeDict)
                    {
                        innerCopy.Add(innerKvp.Key, innerKvp.Value);
                    }
                    
                    result.Add(targetID, innerCopy);
                }

                return result;
            }
        }

        /// <summary>
        /// Removes all relationships between characters managed by the provider
        /// </summary>
        public void ClearAllRelationships()
        {
            // Lock for thread-safety
            lock (_lock)
            {
                _relationships.Clear();
            }
            
            _logger.LogInfo("[RelationshipProvider] All relationships cleared");
        }
    }
}