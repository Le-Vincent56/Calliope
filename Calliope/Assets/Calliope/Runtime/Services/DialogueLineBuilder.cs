using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using ILogger = Calliope.Infrastructure.Logging.ILogger;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Builds dialogue lines based on a variety of input parameters, relationships, and scoring strategies
    /// </summary>
    public class DialogueLineBuilder
    {
        private readonly IFragmentScorer _scorer;
        private readonly ISaliencyStrategy _selectionStrategy;
        private readonly TextAssembler _textAssembler;
        private readonly RelationshipModifierApplier _relationshipApplier;
        private readonly RelationshipProvider _relationshipProvider;
        private readonly ISelectionContext _selectionContext;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;
        
        public DialogueLineBuilder(
            IFragmentScorer scorer, 
            ISaliencyStrategy selectionStrategy, 
            TextAssembler textAssembler, 
            RelationshipModifierApplier relationshipApplier, 
            RelationshipProvider relationshipProvider,
            ISelectionContext selectionContext, 
            IEventBus eventBus, 
            ILogger logger)
        {
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
            _selectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
            _textAssembler = textAssembler ?? throw new ArgumentNullException(nameof(textAssembler));
            _relationshipApplier = relationshipApplier ?? throw new ArgumentNullException(nameof(relationshipApplier));
            _relationshipProvider = relationshipProvider ?? throw new ArgumentNullException(nameof(relationshipProvider));
            _selectionContext = selectionContext ?? throw new ArgumentNullException(nameof(selectionContext));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Constructs a dialogue line by scoring and selecting a suitable dialogue fragment from the given candidates,
        /// assembling it with specific speaker and target context, and optionally applying relationship modifiers
        /// </summary>
        /// <param name="candidates">A read-only list of dialogue fragments to evaluate and select from</param>
        /// <param name="speaker">The character initiating the dialogue</param>
        /// <param name="target">The character targeted by the dialogue, if applicable</param>
        /// <param name="applyRelationshipModifiers">Determines whether relationship-based modifications should be applied to the dialogue fragment</param>
        /// <returns>A constructed dialogue line as a string, or null if no valid line could be generated</returns>
        public string BuildLine(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target,
            bool applyRelationshipModifiers = true
        )
        {
            // Exit case - no candidates were provided
            if (candidates == null || candidates.Count == 0)
            {
                _logger.LogWarning("[DialogueBuilder] No candidate fragments provided");
                return null;
            }

            // Exit case - no speaker was provided
            if (speaker == null)
            {
                _logger.LogError("[DialogueLineBuilder] Speaker cannot be null");
                return null;
            }
            
            // Score all the candidates
            IReadOnlyList<(IDialogueFragment fragment, IScoringResult score)> scoredCandidates = ScoreCandidates(candidates, speaker, target);

            StringBuilder logBuilder = new StringBuilder();
            
            // Exit case - there are valid fragments for the speaker
            if (scoredCandidates.Count == 0)
            {
                logBuilder.Append("[DialogueLineBuilder] No valid fragments for speaker '");
                logBuilder.Append(speaker.DisplayName);
                logBuilder.Append("'");
                
                _logger.LogWarning(logBuilder.ToString());
                return null;
            }
            
            // Select the fragment using the strategy
            IDialogueFragment selectedFragment = _selectionStrategy.Select(
                scoredCandidates,
                _selectionContext
            );

            // Exit case - no fragment was selected
            if (selectedFragment == null)
            {
                _logger.LogWarning("[DialogueLineBuilder] Selection strategy returned null");
                return null;
            }
            
            // Mark the fragment as used (for recency tracking)
            _selectionContext.MarkUsed(selectedFragment.ID);

            // Build the log message
            logBuilder.Append("[DialogueLineBuilder] Selected fragment '");
            logBuilder.Append(selectedFragment.ID);
            logBuilder.Append("' for '");
            logBuilder.Append(speaker.DisplayName);
            logBuilder.Append("'");
            _logger.LogInfo(logBuilder.ToString());
            
            // Assemble the final text
            string assembledText = _textAssembler.Assemble(
                selectedFragment.Text,
                speaker,
                target
            );
            
            // Apply relationship modifiers (if enabled)
            if (applyRelationshipModifiers)
                _relationshipApplier.ApplyModifiers(selectedFragment, speaker, target);
            
            // Publish the event
            _eventBus.Publish(new LinePreparedEvent(
                speaker,
                target,
                assembledText,
                selectedFragment
            ));
            
            return assembledText;
        }

        /// <summary>
        /// Scores a list of dialogue fragment candidates based on their suitability in the provided context
        /// by evaluating each fragment and filtering out invalid results
        /// </summary>
        /// <param name="candidates">A read-only list of dialogue fragments to evaluate and score</param>
        /// <param name="speaker">The character initiating the dialogue</param>
        /// <param name="target">The character targeted by the dialogue, if any</param>
        /// <returns>A list of tuples where each tuple contains a dialogue fragment and its associated scoring result</returns>
        private List<(IDialogueFragment fragment, IScoringResult score)> ScoreCandidates(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target
        )
        {
            List<(IDialogueFragment fragment, IScoringResult score)> results = new List<(IDialogueFragment, IScoringResult)>();
            
            // Create the scoring context
            IScoringContext context = new ScoringContext(
                speaker, 
                target, 
                _relationshipProvider, 
                null
            );
            
            StringBuilder debugBuilder = new StringBuilder();
            
            // Score each candidate
            for(int i = 0; i < candidates.Count; i++)
            {
                IDialogueFragment fragment = candidates[i];
                IScoringResult result = _scorer.Score(fragment, context);
                
                // Add the result if it's valid
                if(result.IsValid) results.Add((fragment, result));
                else
                {
                    // Log the error
                    debugBuilder.Clear();
                    debugBuilder.Append("[DialogueLineBuilder] Invalid fragment '");
                    debugBuilder.Append(fragment.ID);
                    debugBuilder.Append("': ");
                    debugBuilder.Append(result.Explanation);
                    _logger.LogDebug(debugBuilder.ToString());
                }
            }
            
            return results;
        }

        /// <summary>
        /// Creates a dialogue line by selecting and scoring candidates based on the provided context,
        /// applying any relevant modifiers, and preparing the resulting text
        /// </summary>
        /// <param name="candidates">A list of dialogue fragments to consider for the line</param>
        /// <param name="speaker">The character delivering the dialogue line</param>
        /// <param name="target">The target character for the dialogue, if applicable</param>
        /// <param name="applyRelationshipModifiers">Indicates whether relationship modifiers should be applied during line creation</param>
        /// <returns>A tuple containing the assembled dialogue text and the scoring result of the selected dialogue fragment</returns>
        public (string text, IScoringResult result) BuildLineWithDetails(
            IReadOnlyList<IDialogueFragment> candidates,
            ICharacter speaker,
            ICharacter target,
            bool applyRelationshipModifiers = true
        )
        {
            // Exit case - no candidates were provided
            if (candidates == null || candidates.Count == 0)
                return (null, null);
            
            // Exit case - no speaker was provided
            if (speaker == null)
                return (null, null);
            
            // Score all candidates
            List<(IDialogueFragment fragment, IScoringResult score)> scoredCandidates = ScoreCandidates(candidates, speaker, target);
            
            // Exit case - there are no valid candidates
            if(scoredCandidates.Count == 0)
                return (null, null);
            
            // Select the fragment
            IDialogueFragment selectedFragment = _selectionStrategy.Select(
                scoredCandidates,
                _selectionContext
            );
            
            // Exit case - no fragment was selected
            if (selectedFragment == null)
                return (null, null);
            
            // Get the scoring result for the selected fragment
            IScoringResult selectedResult = null;
            for (int i = 0; i < scoredCandidates.Count; i++)
            {
                // Skip if the selected fragment is not the current candidate
                if (scoredCandidates[i].Item1.ID != selectedFragment.ID) continue;
                
                selectedResult = scoredCandidates[i].Item2;
                break;
            }
            
            // Mark as used
            _selectionContext.MarkUsed(selectedFragment.ID);
            
            // Assemble the text
            string assembledText = _textAssembler.Assemble(
                selectedFragment.Text,
                speaker, 
                target
            );
            
            // Apply modifiers
            if (applyRelationshipModifiers)
                _relationshipApplier.ApplyModifiers(selectedFragment, speaker, target);
            
            // Publish the event
            _eventBus.Publish(new LinePreparedEvent(
                speaker,
                target,
                assembledText,
                selectedFragment
            ));
            
            return (assembledText, selectedResult);
        }

        /// <summary>
        /// Sets a custom variable identified by the specified key and value in the text assembly context,
        /// enabling its use in the text substitution process
        /// </summary>
        /// <param name="key">The unique key representing the variable to be set in the context</param>
        /// <param name="value">The value to associate with the specified key in the context</param>
        public void SetVariable(string key, string value) => _textAssembler.SetVariable(key, value);

        /// <summary>
        /// Removes a custom variable identified by the specified key from the text assembly context,
        /// ensuring that it is no longer used in the text substitution process
        /// </summary>
        /// <param name="key">The identifier for the variable to be cleared from the context</param>
        public void ClearVariable(string key) => _textAssembler.ClearVariable(key);
    }
}