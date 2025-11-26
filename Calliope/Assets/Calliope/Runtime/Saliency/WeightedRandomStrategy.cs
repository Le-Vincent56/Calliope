using System.Collections.Generic;
using Calliope.Core.Attributes;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Saliency
{
    /// <summary>
    /// Implements a weighted random saliency strategy for selecting dialogue fragments
    /// based on their scores
    /// </summary>
    [SaliencyStrategy("weighted-random", "Weighted Random")]
    public class WeightedRandomStrategy : ISaliencyStrategy
    {
        public string StrategyID => "weighted-random";
        public string DisplayName => "Weighted Random";

        /// <summary>
        /// Selects a dialogue fragment based on weighted random strategy, considering candidate scores
        /// and contextual factors such as recency penalties
        /// </summary>
        /// <param name="scoredCandidates">
        /// A read-only list of tuples, where each tuple contains an IDialogueFragment and its associated
        /// scoring result; the scoring result determines the weight of the fragment for selection
        /// </param>
        /// <param name="context">
        /// The selection context that provides utilities such as random number generator and recency
        /// tracking for applying penalties or managing usage
        /// </param>
        /// <returns>
        /// The selected IDialogueFragment based on weighted random selection; returns null if there are no
        /// valid candidates, or falls back to a defined default when all scores are negative
        /// </returns>
        public IDialogueFragment Select(
            IReadOnlyList<(IDialogueFragment fragment, IScoringResult score)> scoredCandidates,
            ISelectionContext context
        )
        {
            // Filter to valid candidates (score >= 0)
            List<(IDialogueFragment fragment, float adjustedScore)> valid = new List<(IDialogueFragment fragment, float adjustedScore)>();
            for (int i = 0; i < scoredCandidates.Count; i++)
            {
                // Skip invalid candidates
                if (!scoredCandidates[i].score.IsValid) continue;
                
                IDialogueFragment fragment = scoredCandidates[i].fragment;
                float score = scoredCandidates[i].score.Score;
                    
                // Apply recency penalty
                if (context.IsRecent(fragment.ID))
                    score *= context.RecencyPenalty;
                    
                valid.Add((fragment, score));
            }

            // Exit case - there are no valid fragments
            if (valid.Count == 0) return null;
            
            // Calculate the total score
            float totalScore = 0f;
            for (int i = 0; i < valid.Count; i++)
            {
                totalScore += valid[i].adjustedScore;
            }

            // Exit case - all fragments have a negative score
            if (totalScore <= 0)
                return valid[0].fragment;
            
            // Weighted random selection
            float roll = (float)context.Random.NextDouble() * totalScore;
            float cumulative = 0f;

            for (int i = 0; i < valid.Count; i++)
            {
                cumulative += valid[i].adjustedScore;
                
                // Exit case - roll is within the cumulative weight
                if (roll <= cumulative)
                    return valid[i].fragment;
            }
            
            // Fallback
            IDialogueFragment fallback = valid[^1].fragment;
            return fallback;
        }
    }
}
