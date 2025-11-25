using Calliope.Core.Enums;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Manages relationships between characters
    /// </summary>
    public interface IRelationshipProvider
    {
        /// <summary>
        /// Get the directional relationship value from one character to another;
        /// Example: GetRelationship("aldric", "sera", Opinion) might return 75
        /// </summary>
        /// <param name="fromCharacterID">The unique identifier of the source character</param>
        /// <param name="toCharacterID">The unique identifier of the target character</param>
        /// <param name="type">The type of relationship to evaluate, such as opinion or other relationship types</param>
        /// <returns>A float representing the strength or nature of the relationship</returns>
        float GetRelationship(string fromCharacterID, string toCharacterID, RelationshipType type);

        /// <summary>
        /// Set the relationship to a specific value; the value is automatically clamped to 0-100
        /// </summary>
        /// <param name="fromCharacterID">The unique identifier of the source character</param>
        /// <param name="toCharacterID">The unique identifier of the target character</param>
        /// <param name="type">The type of relationship to set, such as opinion or other relationship types</param>
        /// <param name="value">The value representing the strength or nature of the relationship</param>
        void SetRelationship(string fromCharacterID, string toCharacterID, RelationshipType type, float value);

        /// <summary>
        /// Modify the relationship value by a delta amount;
        /// Example: ModifyRelationship("aldric", "sera", Opinion, 25) would increase the relationship value from 75 to 100
        /// </summary>
        /// <param name="fromCharacterID">The unique identifier of the source character</param>
        /// <param name="toCharacterID">The unique identifier of the target character</param>
        /// <param name="type">The type of relationship to modify, such as opinion or other relationship types</param>
        /// <param name="delta">The change to apply to the current relationship value</param>
        void ModifyRelationship(string fromCharacterID, string toCharacterID, RelationshipType type, float delta);
    }
}