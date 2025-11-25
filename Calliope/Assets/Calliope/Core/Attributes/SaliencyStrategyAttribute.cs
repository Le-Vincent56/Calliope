using System;

namespace Calliope.Core.Attributes
{
    /// <summary>
    /// Marks a class as a saliency strategy for automatic discovery, apply to strategy implementations:
    /// [SaliencyStrategy("weighted_random", "Weighted Random")]
    /// public class WeightedRandomStrategy : ISaliencyStrategy { }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SaliencyStrategyAttribute : Attribute
    {
        /// <summary>
        /// Unique identifier for the strategy
        /// </summary>
        public string StrategyID { get; }
        
        /// <summary>
        /// A display name for editor dropdowns
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        /// An optional description of what this strategy does
        /// </summary>
        public string Description { get; }

        public SaliencyStrategyAttribute(string strategyID, string displayName = null, string description = null)
        {
            StrategyID = strategyID;
            DisplayName = displayName;
            Description = description;
        }
    }
}