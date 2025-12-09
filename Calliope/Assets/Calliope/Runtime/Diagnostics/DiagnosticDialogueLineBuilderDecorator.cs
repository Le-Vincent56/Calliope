using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using Calliope.Runtime.Diagnostics.Events;
using Calliope.Runtime.Diagnostics.Models;
using Calliope.Runtime.Services;

namespace Calliope.Runtime.Diagnostics
{
    /// <summary>
    /// Decorator that wraps DialogueLineBuilder to capture and publish diagnostic events
    /// for fragment scoring and selection
    /// </summary>
    public class DiagnosticDialogueLineBuilderDecorator : IDialogueLineBuilder
    {
        private readonly DialogueLineBuilder _inner;
        private readonly IFragmentScorer _scorer;
        private readonly IRelationshipProvider _relationshipProvider;
        private readonly ISaliencyStrategy _selectionStrategy;
        private readonly IEventBus _eventBus;
        private readonly IDiagnosticsManager _diagnosticsManager;

        private string _currentBeatID;
        private string _currentVariationSetID;
        private string _capturedSelectedFragmentID;
        private bool _isCapturing;

        public DiagnosticDialogueLineBuilderDecorator(
            DialogueLineBuilder inner,
            IFragmentScorer scorer,
            ISaliencyStrategy selectionStrategy,
            IRelationshipProvider relationshipProvider,
            IEventBus eventBus,
            IDiagnosticsManager diagnosticsManager
        )
        {
            _inner = inner;
            _scorer = scorer;
            _selectionStrategy = selectionStrategy;
            _relationshipProvider = relationshipProvider;
            _eventBus = eventBus;
            _diagnosticsManager = diagnosticsManager;
        }

        /// <summary>
        /// Sets the current context for the beat and variation set by updating internal identifiers
        /// </summary>
        /// <param name="beatID">The unique identifier for the current beat</param>
        /// <param name="variationSetID">The unique identifier for the current variation set</param>
        public void SetBeatContext(string beatID, string variationSetID)
        {
            _currentBeatID = beatID;
            _currentVariationSetID = variationSetID;
        }

        /// <summary>
        /// Constructs a dialogue line based on provided dialogue fragments, speaker, target, and optionally
        /// applies relationship modifiers; additionally, records diagnostic information if diagnostics are enabled
        /// </summary>
        /// <param name="candidates">A read-only list of dialogue fragments to choose from for constructing the line</param>
        /// <param name="speaker">The character delivering the dialogue line</param>
        /// <param name="target">The character to whom the dialogue line is directed</param>
        /// <param name="applyRelationshipModifiers">Specifies whether relationship-based modifiers should be applied to the generated line</param>
        /// <returns>The constructed dialogue line as a string</returns>
        public string BuildLine(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target,
            bool applyRelationshipModifiers = true
        )
        {
            // If diagnostics disabled, call the inner function
            if (!_diagnosticsManager.IsEnabled)
                return _inner.BuildLine(candidates, speaker, target, applyRelationshipModifiers);

            // Score ALL candidates (including invalid ones) for diagnostics
            List<CandidateScoringResult> allCandidateResults = ScoreAllCandidates(candidates, speaker, target);

            // Subscribe to LinePreparedEvent to capture which fragment was selected
            _capturedSelectedFragmentID = null;
            _isCapturing = true;
            _eventBus.Subscribe<LinePreparedEvent>(OnLinePrepared);
            
            // Call the actual build
            string assembledText;
            try
            {
                assembledText = _inner.BuildLine(candidates, speaker, target, applyRelationshipModifiers);
            }
            finally
            {
                // Always unsubscribe
                _eventBus.Unsubscribe<LinePreparedEvent>(OnLinePrepared);
                _isCapturing = false;
            }
            
            // Mark the selected fragment
            MarkSelectedFragment(allCandidateResults, _capturedSelectedFragmentID);

            // Publish diagnostic event
            _eventBus.Publish(new DiagnosticFragmentSelectionEvent(
                _currentBeatID,
                _currentVariationSetID,
                speaker,
                target,
                allCandidateResults,
                _capturedSelectedFragmentID,
                _selectionStrategy.DisplayName,
                assembledText
            ));

            return assembledText;
        }

        /// <summary>
        /// Scores all provided dialogue candidates based on the interaction between a speaker and a target, producing a list of scoring results
        /// </summary>
        /// <param name="candidates">The collection of dialogue fragments to be scored</param>
        /// <param name="speaker">The character initiating the dialogue</param>
        /// <param name="target">The character receiving the dialogue</param>
        /// <returns>A list of candidate scoring results containing scores and associated details for each fragment</returns>
        private List<CandidateScoringResult> ScoreAllCandidates(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target
        )
        {
            List<CandidateScoringResult> results = new List<CandidateScoringResult>();

            // Exit case - no candidates were provided
            if (candidates == null || candidates.Count == 0)
                return results;

            IScoringContext context = new ScoringContext(speaker, target, _relationshipProvider, null);

            // Score each candidate
            for (int i = 0; i < candidates.Count; i++)
            {
                IDialogueFragment fragment = candidates[i];
                IScoringResult scoreResult = _scorer.Score(fragment, context);

                // Add the result
                results.Add(new CandidateScoringResult(
                    fragment.ID,
                    fragment.Text,
                    scoreResult.Score,
                    scoreResult.IsValid,
                    scoreResult.Explanation,
                    wasSelected: false
                ));
            }

            return results;
        }

        /// <summary>
        /// Marks the dialogue fragment that was selected by updating its metadata in the candidate results list
        /// </summary>
        /// <param name="candidates">The list of candidate scoring results, each containing fragment-related scoring and metadata</param>
        /// <param name="selectedID">The unique identifier of the fragment that was selected for the dialogue line</param>
        private void MarkSelectedFragment(List<CandidateScoringResult> candidates, string selectedID)
        {
            // Exit case - no fragment was selected
            if (string.IsNullOrEmpty(selectedID))
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                // Skip if the candidate is not the selected one
                if (candidates[i].FragmentID != selectedID) continue;
                
                // Create a new result with wasSelected = true
                CandidateScoringResult original = candidates[i];
                candidates[i] = new CandidateScoringResult(
                    original.FragmentID,
                    original.FragmentText,
                    original.Score,
                    original.IsValid,
                    original.ScoringExplanation,
                    wasSelected: true
                );
                break;
            }
        }

        /// <summary>
        /// Handles the event triggered when a dialogue line is prepared, capturing the selected fragment ID for diagnostics purposes
        /// </summary>
        /// <param name="evt">The event containing details about the prepared line and the selected dialogue fragment</param>
        private void OnLinePrepared(LinePreparedEvent evt)
        {
            // Exit case - no capturing or no fragment selected
            if (!_isCapturing || evt.SelectedFragment == null) return;
            
            _capturedSelectedFragmentID = evt.SelectedFragment.ID;
        }

        /// <summary>
        /// Sets a custom variable identified by the specified key and value in the context of the dialogue line builder,
        /// enabling its use in the text assembly process through the decorated instance
        /// </summary>
        /// <param name="key">The unique key identifying the variable to be set</param>
        /// <param name="value">The value to associate with the specified key</param>
        public void SetVariable(string key, string value) => _inner.SetVariable(key, value);

        /// <summary>
        /// Removes a variable identified by the specified key, ensuring it is no longer available in the dialogue context
        /// </summary>
        /// <param name="key">The identifier for the variable to be cleared from the dialogue context</param>
        public void ClearVariable(string key) => _inner.ClearVariable(key);

        /// <summary>
        /// Builds a dialogue line with additional scoring details based on the provided candidates, speaker, and target, optionally applying relationship modifiers
        /// </summary>
        /// <param name="candidates">The list of dialogue fragments to consider for constructing the line</param>
        /// <param name="speaker">The character who is delivering the dialogue line</param>
        /// <param name="target">The character who is the target of the dialogue line</param>
        /// <param name="applyRelationshipModifiers">A flag indicating whether relationship-based scoring modifiers should be applied</param>
        /// <returns>A tuple containing the constructed dialogue line text and its corresponding scoring result</returns>
        public (string text, IScoringResult result) BuildLineWithDetails(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target,
            bool applyRelationshipModifiers = true
        )
        {
            // If diagnostics disabled, call the inner function
            if (!_diagnosticsManager.IsEnabled)
                return _inner.BuildLineWithDetails(candidates, speaker, target, applyRelationshipModifiers);
            
            List<CandidateScoringResult> allCandidateResults = ScoreAllCandidates(candidates, speaker, target);
            
            // Subscribe to LinePreparedEvent to capture which fragment was selected
            _capturedSelectedFragmentID = null;
            _isCapturing = true;
            _eventBus.Subscribe<LinePreparedEvent>(OnLinePrepared);

            (string text, IScoringResult score) buildResult;
            try
            {
                buildResult = _inner.BuildLineWithDetails(candidates, speaker, target, applyRelationshipModifiers);
            }
            finally
            {
                // Always unsubscribe
                _eventBus.Unsubscribe<LinePreparedEvent>(OnLinePrepared);
                _isCapturing = false;
            }
            
            // Mark the selected fragment
            MarkSelectedFragment(allCandidateResults, _capturedSelectedFragmentID);

            // Publish event (simplified - would need similar logic as BuildLine)
            _eventBus.Publish(new DiagnosticFragmentSelectionEvent(
                _currentBeatID,
                _currentVariationSetID,
                speaker,
                target,
                allCandidateResults,
                _capturedSelectedFragmentID,
                _selectionStrategy.DisplayName,
                buildResult.text
            ));

            return buildResult;
        }
    }
}