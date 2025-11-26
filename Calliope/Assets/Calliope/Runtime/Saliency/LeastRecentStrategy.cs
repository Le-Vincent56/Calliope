using System.Collections.Generic;
using Calliope.Core.Attributes;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Saliency
{
    [SaliencyStrategy("least-recent", "Least Recent")]
    public class LeastRecentStrategy : ISaliencyStrategy
    {
        public string StrategyID => "least-recent";
        public string DisplayName => "Least Recent";

        public IDialogueFragment Select(
            IReadOnlyList<(IDialogueFragment fragment, IScoringResult score)> scoredCandidates,
            ISelectionContext context
        )
        {
            // Filter valid candidates
            List<(IDialogueFragment IDialogueFragment, float score)> valid = new List<(IDialogueFragment IDialogueFragment, float score)>();
            for (int i = 0; i < scoredCandidates.Count; i++)
            {
                // Skip over invalid candidates
                if (!scoredCandidates[i].score.IsValid) continue;
                
                valid.Add((scoredCandidates[i].fragment, scoredCandidates[i].score.Score));
            }

            // Exit case - there are no valid candidates
            if (valid.Count == 0) return null;
            
            // Categorize by usage
            List<(IDialogueFragment fragment, float score)> neverUsed = new List<(IDialogueFragment IDialogueFragment, float score)>();
            List<(IDialogueFragment fragment, float score)> notRecent = new List<(IDialogueFragment IDialogueFragment, float score)>();
            List<(IDialogueFragment fragment, float score)> recent = new List<(IDialogueFragment IDialogueFragment, float score)>();

            for (int i = 0; i < valid.Count; i++)
            {
                (IDialogueFragment fragment, float score) item = valid[i];
                
                // Get the fragment's use count and determine whether it's recent
                int useCount = context.GetUseCount(item.fragment.ID);
                bool isRecent = context.IsRecent(item.fragment.ID);
                
                // Categorize the fragment
                if(useCount == 0) neverUsed.Add(item);
                else if(!isRecent) notRecent.Add(item);
                else recent.Add(item);
            }
            
            // Prefer never-used, then not-recent, then recent
            List<(IDialogueFragment fragment, float score)> pool = neverUsed.Count > 0
                ? neverUsed
                : notRecent.Count > 0
                    ? notRecent
                    : recent;
            
            // Calcualte the total score
            float totalScore = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalScore += pool[i].score;
            }

            // If all fragments have a negative score, return the first one
            if (totalScore <= 0)
            {
                context.MarkUsed(pool[0].fragment.ID);
                return pool[0].fragment;
            }
            
            // Weighted random roll selection
            float roll = (float)context.Random.NextDouble() * totalScore;
            float cumulative = 0f;

            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += pool[i].score;
                
                // Exit case - roll is within the cumulative weight
                if (roll <= cumulative)
                {
                    context.MarkUsed(pool[i].fragment.ID);
                    return pool[i].fragment;
                }
            }
            
            // Fallback
            IDialogueFragment fallback = pool[^1].fragment;
            context.MarkUsed(fallback.ID);
            return fallback;
        }
    }
}