using System.Collections.Generic;
using Calliope.Core.Attributes;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Saliency
{
    /// <summary>
    /// Represents a saliency strategy that selects the dialogue fragment with the highest score from a list of scored candidates
    /// </summary>
    [SaliencyStrategy("highest-score", "Highest Score")]
    public class HighestScoreStrategy : ISaliencyStrategy
    {
        public string StrategyID => "highest-score";
        public string DisplayName => "Highest Score";

        /// <summary>
        /// Selects the dialogue fragment with the highest score from a list of scored candidates
        /// </summary>
        /// <param name="scoredCandidates">
        /// A read-only list of tuples, where each tuple contains a dialogue fragment
        /// and its corresponding scoring result
        /// </param>
        /// <param name="context">
        /// The context used for managing the selection process, such as tracking already
        /// used fragments
        /// </param>
        /// <returns>The dialogue fragment with the highest score, or null if no valid fragments are found</returns>
        public IDialogueFragment Select(
            IReadOnlyList<(IDialogueFragment fragment, IScoringResult score)> scoredCandidates,
            ISelectionContext context
        )
        {
            IDialogueFragment best = null;
            float maxScore = float.MinValue;
            
            // Find the highest scoring fragment
            for (int i = 0; i < scoredCandidates.Count; i++)
            {
                // Skip if the score is invalid
                if (!scoredCandidates[i].score.IsValid) continue;
                
                // Skip if the score is lower than or equal to the current best
                if (scoredCandidates[i].score.Score <= maxScore) continue;
                
                // Update the best
                maxScore = scoredCandidates[i].score.Score;
                best = scoredCandidates[i].fragment;
            }

            // Exit case - no valid fragments
            if (best == null) return null;
            
            return best;
        }
    }
}