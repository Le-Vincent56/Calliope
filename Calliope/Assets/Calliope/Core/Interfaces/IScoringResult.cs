namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// The result of scoring a dialogue fragment; contains the score
    /// and a human-readable explanation of how it was calculated
    /// </summary>
    public interface IScoringResult
    {
        /// <summary>
        /// The final score; negative means the result is invalid/unavailable,
        /// a positive, higher score means it is a better match for the speaker
        /// </summary>
        float Score { get; }
        
        /// <summary>
        /// Whether this fragment is valid for the speaker;
        /// score >= 0 means it is valid
        /// </summary>
        bool IsValid { get; }
        
        /// <summary>
        /// Human-readable explanation of how the score was calculated;
        /// Example:
        /// "Base score: 1.0,
        /// Trait 'aggressive' (+1.0): 2.0
        /// Relationship >= 70 (x1.5): 3.0"
        /// </summary>
        string Explanation { get; }
    }
}