using System;

namespace Calliope.Core.ValueObjects
{
    /// <summary>
    /// Operations that can be performed on context values
    /// </summary>
    public enum ContextModifierOperation
    {
        Set,
        Increment,
        Decrement,
        SetTrue,
        SetFalse,
        Multiply,
        Max,
        Min
    }
    
    /// <summary>
    /// Defines how a dialogue fragment modifies scene context when selected;
    /// enables data-driven scene state tracking without programmer intervention
    /// </summary>
    [Serializable]
    public struct ContextModifier
    {
        /// <summary>
        /// The context key to modify (e.g., "tension_level", "war_influence", "secret_hinted")
        /// </summary>
        public string Key;

        /// <summary>
        /// How to modify the value
        /// </summary>
        public ContextModifierOperation Operation;

        /// <summary>
        /// The value to use for the operation (for Set, Increment, Decrement)
        /// </summary>
        public float Value;

        public ContextModifier(string key, ContextModifierOperation operation, float value = 0f)
        {
            Key = key;
            Operation = operation;
            Value = value;
        }
    }
}
