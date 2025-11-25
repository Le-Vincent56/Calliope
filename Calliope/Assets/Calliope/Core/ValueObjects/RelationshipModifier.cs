using Calliope.Core.Enums;

namespace Calliope.Core.ValueObjects
{
    /// <summary>
    /// Modifiers fragment score based on the relationship with the target;
    /// For example, "I trust you" requires a high opinion of the target
    /// </summary>
    [System.Serializable]
    public struct RelationshipModifier
    {
        public RelationshipType Type;
        public float Threshold;             // The Relationship must be >= this value
        public float Multiplier;            // Score multiplier if the threshold is met

        public RelationshipModifier(RelationshipType type, float threshold, float multiplier)
        {
            Type = type;
            Threshold = threshold;
            Multiplier = multiplier;
        }
    }
}