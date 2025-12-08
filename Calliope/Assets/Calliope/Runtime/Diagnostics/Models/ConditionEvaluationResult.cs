namespace Calliope.Runtime.Diagnostics.Models
{
    /// <summary>
    /// Represents the evaluation result of a single branch condition
    /// </summary>
    public class ConditionEvaluationResult
    {
        /// <summary>
        /// The type of condition (e.g., "TraitCondition", "RelationshipCondition")
        /// </summary>
        public string ConditionType { get; }
        
        /// <summary>
        /// Human-readable description from IBranchCondition.GetDescription()
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Whether this condition passed evaluation
        /// </summary>
        public bool Passed { get; }
        
        /// <summary>
        /// Detailed explanation of why the condition or failed
        /// </summary>
        public string Details { get; }

        public ConditionEvaluationResult(
            string conditionType, 
            string description, 
            bool passed, 
            string details)
        {
            ConditionType = conditionType;
            Description = description;
            Passed = passed;
            Details = details;
        }
    }
}
