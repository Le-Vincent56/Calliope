using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;
using Calliope.Infrastructure.Caching;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Scores dialogue fragments based on trait affinities and relationship modifiers;
    /// uses caching to avoid redundant calculations
    /// </summary>
    public class FragmentScorer : IFragmentScorer
    {
        private readonly ScoringCache _cache;

        public FragmentScorer(ScoringCache cache = null)
        {
            _cache = cache ?? new ScoringCache();
        }

        /// <summary>
        /// Calculates and retrieves a scoring result for a given dialogue fragment
        /// in the specified scoring context, utilizing cached results to optimize performance
        /// </summary>
        /// <param name="fragment">
        /// The dialogue fragment to be scored, uniquely identified by its ID
        /// </param>
        /// <param name="context">
        /// The scoring context containing information about the speaker and the optional target,
        /// along with their respective traits and relationships that influence the scoring process
        /// </param>
        /// <returns>
        /// An object that implements the IScoringResult interface, containing the calculated
        /// score and validity information for the provided dialogue fragment
        /// </returns>
        public IScoringResult Score(IDialogueFragment fragment, IScoringContext context)
        {
            // Try to retrieve the scoring result from the cache
            string targetID = context.Target?.ID ?? "";
            if (_cache.TryGet(fragment.ID, context.Speaker.ID, targetID, out IScoringResult cachedResult))
                return cachedResult;
            
            // If the cache doesn't hold the score, compute the score
            IScoringResult result = ScoreInternal(fragment, context);
            
            // Add to the cache
            _cache.Add(fragment.ID, context.Speaker.ID, targetID, result);
            
            return result;
        }

        /// <summary>
        /// Calculates and provides a scoring result for a given dialogue fragment
        /// based on the provided scoring context, including compatibility with traits,
        /// relationships, and other specified attributes
        /// </summary>
        /// <param name="fragment">
        /// The dialogue fragment that needs to be scored, containing information about
        /// required traits, forbidden traits, and affinities impacting the scoring process
        /// </param>
        /// <param name="context">
        /// The scoring context containing information about the speaker, optional target,
        /// and their respective traits, relationships, and other custom factors influencing
        /// the score calculation
        /// </param>
        /// <returns>
        /// The resulting scoring evaluation for the provided dialogue fragment, encapsulated
        /// as an object that implements the IScoringResult interface
        /// </returns>
        private IScoringResult ScoreInternal(IDialogueFragment fragment, IScoringContext context)
        {
            ScoringExplanationBuilder explanationBuilder = new ScoringExplanationBuilder();
            StringBuilder reasonBuilder = new StringBuilder();
            
            // Check hard requirements first
            for (int i = 0; i < fragment.RequiredTraitIDs.Count; i++)
            {
                // Skip if the speaker has the trait
                if (context.Speaker.HasTrait(fragment.RequiredTraitIDs[i])) continue;
                
                // Build the reason
                reasonBuilder.Clear();
                reasonBuilder.Append("Speaker does not have required trait ");
                reasonBuilder.Append(fragment.RequiredTraitIDs[i]);
                    
                // Mark the fragment as invalid
                explanationBuilder.MarkInvalid(reasonBuilder.ToString());
                return explanationBuilder.Build();
            }
            
            // Check forbidden traits
            for (int i = 0; i < fragment.ForbiddenTraitIDs.Count; i++)
            {
                // Skip if the speaker does not have the trait
                if (!context.Speaker.HasTrait(fragment.ForbiddenTraitIDs[i])) continue;
                
                // Build the reason
                reasonBuilder.Clear();
                reasonBuilder.Append("Speaker has forbidden trait ");
                reasonBuilder.Append(fragment.ForbiddenTraitIDs[i]);
                    
                // Mark the fragment as invalid
                explanationBuilder.MarkInvalid(reasonBuilder.ToString());
                return explanationBuilder.Build();
            }
            
            // Start with the base score
            explanationBuilder.SetBaseScore(1.0f);
            
            // Apply trait affinities
            for (int i = 0; i < fragment.TraitAffinities.Count; i++)
            {
                // Get the trait affinity
                TraitAffinity affinity = fragment.TraitAffinities[i];

                // Check if the speaker has the trait
                bool hasTrait = context.Speaker.HasTrait(affinity.TraitID);
                
                // Apply the trait affinity
                explanationBuilder.ApplyTraitAffinity(affinity.TraitID, affinity.Weight, hasTrait);
            }
            
            // Apply relationship modifiers (if target specified)
            if (context.Target != null)
            {
                for (int i = 0; i < fragment.RelationshipModifiers.Count; i++)
                {
                    // Get the relationship modifier
                    RelationshipModifier modifier = fragment.RelationshipModifiers[i];
                    
                    // Get the relationship between the speaker and the target
                    float relationship = context.Relationships.GetRelationship(
                        context.Speaker.ID, 
                        context.Target.ID, 
                        modifier.Type
                    );
                    
                    // Apply the modifier if the relationship meets the threshold
                    bool met = relationship >= modifier.Threshold;
                    explanationBuilder.ApplyRelationshipModifier(modifier.Threshold, modifier.Multiplier, relationship, met);
                }
            }
            
            return explanationBuilder.Build();
        }

        /// <summary>
        /// Scores a batch of dialogue fragments asynchronously based on their
        /// trait affinities, relationship modifiers, and the provided scoring context
        /// </summary>
        /// <param name="fragments">A read-only list of dialogue fragments to be scored</param>
        /// <param name="context">
        /// The scoring context containing details about the speaker, target, relationships,
        /// and custom data used for scoring
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation; the task result contains a
        /// read-only list of scoring results for the provided fragments
        /// </returns>
        public async Task<IReadOnlyList<IScoringResult>> ScoreBatchAsync(
            IReadOnlyList<IDialogueFragment> fragments,
            IScoringContext context
        )
        {
            // For v1.0, just run synchronously
            List<IScoringResult> results = new List<IScoringResult>(fragments.Count);

            for (int i = 0; i < fragments.Count; i++)
            {
                results.Add(Score(fragments[i], context));
            }
            return await Task.FromResult(results);
        }

        /// <summary>
        /// Clears all cached scoring results maintained by the FragmentScorer
        /// </summary>
        public void ClearCache() => _cache.Clear();
    }
}