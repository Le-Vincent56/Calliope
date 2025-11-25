using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Scores dialogue fragments based on character context; uses trait affinities,
    /// relationship modifiers, and requirements
    /// </summary>
    public interface IFragmentScorer
    {
        /// <summary>
        /// Score a single fragment for a speaker; returns negative score if
        /// fragment is invalid (missing required traits, etc.)
        /// </summary>
        /// <param name="fragment">The dialogue fragment to be scored, containing text, trait affinities, and constraints</param>
        /// <param name="context">The context in which scoring is performed, including the speaker, target, relationships, and custom data</param>
        /// <returns>Returns an IScoringResult containing the score, a validity flag, and an explanation of the scoring process</returns>
        IScoringResult Score(IDialogueFragment fragment, IScoringContext context);


        /// <summary>
        /// Score multiple fragments in a batch (can be parallelized); useful for scoring all variations
        /// in a set at once
        /// </summary>
        /// <param name="fragments">
        /// The collection of dialogue fragments to be scored, each containing text, trait affinities, and constraints
        /// </param>
        /// <param name="context">
        /// The context in which scoring is performed, including details about the speaker, target, relationships, and custom data
        /// </param>
        /// <returns>
        /// Returns a task that resolves to a read-only list of IScoringResult objects, each containing the score, a validity flag, and an explanation
        /// of the scoring process for an individual fragment
        /// </returns>
        Task<IReadOnlyList<IScoringResult>> ScoreBatchAsync(IReadOnlyList<IDialogueFragment> fragments, IScoringContext context);
    }

    public interface ISaliencyStrategy
    {
        /// <summary>
        /// Unique identifier ("weighted-random")
        /// </summary>
        string StrategyID { get; }
        
        /// <summary>
        /// Human-readable name ("Weighted Random Strategy")
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Select one fragment from a list of scored candidates
        /// </summary>
        /// <param name="scoredCandidates">A read-only list of tuples, where each tuple contains a dialogue fragment and its respective scoring result</param>
        /// <param name="context">The selection context providing additional data or constraints to influence the fragment selection</param>
        /// <returns>Returns the selected dialogue fragment that best fits the selection criteria</returns>
        IDialogueFragment Select(
            IReadOnlyList<(IDialogueFragment fragment, IScoringResult score)> scoredCandidates,
            ISelectionContext context
        );
    }
}