using System;
using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Runtime.Diagnostics.Models;

namespace Calliope.Runtime.Diagnostics.Events
{
    /// <summary>
    /// Published when a fragment is selected, containing full scoring breakdown
    /// for all candidates
    /// </summary>
    public class DiagnosticFragmentSelectionEvent
    {
        /// <summary>
        /// When this event was created (UTC)
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// The beat ID where this selection occurred
        /// </summary>
        public string BeatID { get; }

        /// <summary>
        /// The variation set ID that provided the candidates
        /// </summary>
        public string VariationSetID { get; }

        /// <summary>
        /// The character speaking this line
        /// </summary>
        public ICharacter Speaker { get; }

        /// <summary>
        /// The character being spoken to (can be null)
        /// </summary>
        public ICharacter Target { get; }

        /// <summary>
        /// Scoring results for ALL candidates (valid and invalid)
        /// </summary>
        public IReadOnlyList<CandidateScoringResult> AllCandidates { get; }

        /// <summary>
        /// The ID of the fragment that was selected
        /// </summary>
        public string SelectedFragmentID { get; }

        /// <summary>
        /// The name of the selection strategy used
        /// </summary>
        public string SelectionStrategyUsed { get; }

        /// <summary>
        /// The final assembled text after variable substitution
        /// </summary>
        public string AssembledText { get; }

        public DiagnosticFragmentSelectionEvent(
            string beatID,
            string variationSetID,
            ICharacter speaker,
            ICharacter target,
            IReadOnlyList<CandidateScoringResult> allCandidates,
            string selectedFragmentID,
            string selectionStrategyUsed,
            string assembledText
        )
        {
            Timestamp = DateTime.UtcNow;
            BeatID = beatID;
            VariationSetID = variationSetID;
            Speaker = speaker;
            Target = target;
            AllCandidates = allCandidates ?? new List<CandidateScoringResult>();
            SelectedFragmentID = selectedFragmentID;
            SelectionStrategyUsed = selectionStrategyUsed;
            AssembledText = assembledText;
        }
    }
}