namespace Calliope.Core.Enums
{
    /// <summary>
    /// How to compare a scene context value against a target
    /// </summary>
    public enum ContextValueComparison
    {
        Exists,                 // Key exists in the context
        NotExists,              // Key does not exist in the context
        Equals,                 // Value equals the target (string or numeric)
        NotEquals,              // Value does not equal the target
        GreaterThan,            // Numeric value > target
        GreaterOrEqual,     // Numeric value >= target
        LessThan,               // Numeric value < target
        LessOrEqual,        // Numeric value <= target
        Contains,               // String value contains the target substring
        StartsWith,             // String value starts with the target
        IsTrue,                 // Boolean value is true
        IsFalse                 // Boolean value is false
    }
}