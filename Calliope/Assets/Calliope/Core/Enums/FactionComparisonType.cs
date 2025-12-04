namespace Calliope.Core.Enums
{
    /// <summary>
    /// Hwo to compare faction counts in the cast
    /// </summary>
    public enum FactionComparisonType
    {
        Majority,       // More members than any other faction
        Minority,       // Fewer members than any other faction
        AtLeast,        // At least N members
        Exactly,        // Exactly N members
        MoreThan,       // More than N members
        LessThan        // Fewer than N members
    }
}