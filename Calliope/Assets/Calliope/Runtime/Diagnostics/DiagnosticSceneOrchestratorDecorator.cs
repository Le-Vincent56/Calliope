using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using Calliope.Runtime.Diagnostics.Events;
using Calliope.Runtime.Diagnostics.Models;
using Calliope.Runtime.Services;

namespace Calliope.Runtime.Diagnostics
{
    /// <summary>
    /// Decorator that wraps SceneOrchestrator to capture and publish diagnostic events
    /// for beat transitions and branch evaluations
    /// </summary>
    public class DiagnosticSceneOrchestratorDecorator
    {
        private readonly SceneOrchestrator _inner;
        private readonly IRelationshipProvider _relationshipProvider;
        private readonly IEventBus _eventBus;
        private readonly IDiagnosticsManager _diagnosticsManager;

        public ISceneContext SceneContext => _inner.SceneContext;
        
        public DiagnosticSceneOrchestratorDecorator(
            SceneOrchestrator inner, 
            IRelationshipProvider relationshipProvider,
            IEventBus eventBus, 
            IDiagnosticsManager diagnosticsManager
        )
        {
            _inner = inner;
            _relationshipProvider = relationshipProvider;
            _eventBus = eventBus;
            _diagnosticsManager = diagnosticsManager;
        }

        public bool AdvanceToNextBeat()
        {
            // If diagnostics disabled, call the inner function
            if(!_diagnosticsManager.IsEnabled)
                return _inner.AdvanceToNextBeat();
            
            // Capture state before transition
            ISceneBeat currentBeat = _inner.GetCurrentBeat();
            ISceneTemplate currentScene = _inner.GetCurrentScene();
            IReadOnlyDictionary<string, ICharacter> cast = _inner.GetCurrentCast();
            
            // If there is no current beat or scene, call the inner function
            if(currentBeat == null || currentScene == null)
                return _inner.AdvanceToNextBeat();

            string fromBeatID = currentBeat.BeatID;
            bool wasEndBeat = currentBeat.IsEndBeat;
            
            // Evaluate all branches to capture diagnostic data
            List<BranchEvaluationResult> branchEvaluations = EvaluateAllBranches(
                currentBeat.Branches, 
                cast
            );
            
            // Determine which branch would be taken (if any)
            string branchToBeatID = FindSelectedBranchTarget(branchEvaluations);
            bool usedDefault = string.IsNullOrEmpty(branchToBeatID) && !wasEndBeat;
            
            // Call the actual advance
            bool result = _inner.AdvanceToNextBeat();
            
            // Get the new beat ID (or null if the scene ended)
            string toBeatID = wasEndBeat 
                ? null 
                : _inner.GetCurrentBeat()?.BeatID;
            
            // Publish diagnostic event
            _eventBus.Publish(new DiagnosticBeatTransitionEvent(
                currentScene.ID,
                fromBeatID,
                toBeatID,
                usedDefault,
                wasEndBeat,
                branchEvaluations,
                cast
            ));

            return result;
        }

        /// <summary>
        /// Evaluates all branches for a given scene beat and cast of characters to capture diagnostic data
        /// </summary>
        /// <param name="branches">The list of branches available from the current scene beat</param>
        /// <param name="cast">The collection of characters involved in the current scene, indexed by their identifiers</param>
        /// <returns>A list of results encapsulating the evaluation of each branch, including diagnostic information</returns>
        private List<BranchEvaluationResult> EvaluateAllBranches(
            IReadOnlyList<IBeatBranch> branches,
            IReadOnlyDictionary<string, ICharacter> cast
        )
        {
            List<BranchEvaluationResult> results = new List<BranchEvaluationResult>();

            // Exit case - no branches are passed in
            if (branches == null || branches.Count == 0)
                return results;

            bool foundSelectedBranch = false;
            
            // Evaluate each branch
            for (int i = 0; i < branches.Count; i++)
            {
                IBeatBranch branch = branches[i];
                List<ConditionEvaluationResult> conditionResults = new List<ConditionEvaluationResult>();

                bool isUnconditional = branch.Conditions == null || branch.Conditions.Count == 0;
                bool allConditionsPassed = true;

                if (!isUnconditional)
                {
                    for (int j = 0; j < branch.Conditions.Count; j++)
                    {
                        IBranchCondition condition = branch.Conditions[j];

                        // Evaluate the condition
                        bool passed = condition.Evaluate(cast, _relationshipProvider, _inner.SceneContext);
                        
                        // Build the result
                        string details = BuildConditionDetails(condition, cast, passed);
                        conditionResults.Add(new ConditionEvaluationResult(
                            condition.GetType().Name,
                            condition.GetDescription(),
                            passed,
                            details
                        ));

                        // Check if any conditions failed
                        if (!passed) allConditionsPassed = false;
                    }
                }
                
                // A branch is selected if it's the first one that passes (or is unconditional)
                bool wasSelected = !foundSelectedBranch && (isUnconditional || allConditionsPassed);
                
                // Mark the first branch that passes as selected
                if(wasSelected) foundSelectedBranch = true;
                
                // Build the result
                results.Add(new BranchEvaluationResult(
                    i, 
                    branch.NextBeatID, 
                    wasSelected,
                    allConditionsPassed,
                    isUnconditional,
                    conditionResults
                ));
            }

            return results;
        }

        /// <summary>
        /// Builds a detailed string describing the evaluation outcome of a branch condition,
        /// including its type, description, pass/fail status, and relevant cast context
        /// </summary>
        /// <param name="condition">The condition being evaluated, providing information on type and description</param>
        /// <param name="cast">The collection of characters involved in the evaluation, indexed by their identifiers</param>
        /// <param name="passed">A boolean indicating whether the condition evaluation passed or failed</param>
        /// <returns>A detailed string summarizing the evaluation result of the condition, including its context and outcome</returns>
        private string BuildConditionDetails(
            IBranchCondition condition,
            IReadOnlyDictionary<string, ICharacter> cast,
            bool passed
        )
        {
            StringBuilder detailsBuilder = new StringBuilder();
            detailsBuilder.Append(condition.GetType().Name);
            
            // Build context-specific details based on condition type
            detailsBuilder.Append(passed ? " PASSED: " : " FAILED: ");
            detailsBuilder.Append(condition.GetDescription());
            
            // Add cast context for trait/relationship conditions
            if (cast is { Count: > 0 })
            {
                detailsBuilder.Append(" [Cast: ");
                bool first = true;
                foreach (KeyValuePair<string, ICharacter> kvp in cast)
                {
                    // Add a comma if not the first character
                    if (!first) detailsBuilder.Append(", ");
                    
                    detailsBuilder.Append(kvp.Key);
                    detailsBuilder.Append("=");
                    detailsBuilder.Append(kvp.Value.DisplayName);
                    first = false;
                }
                detailsBuilder.Append("]");
            }

            return detailsBuilder.ToString();
        }

        /// <summary>
        /// Determines the target beat ID of the selected branch from a list of branch evaluations
        /// </summary>
        /// <param name="branchEvaluations">
        /// A list of branch evaluation results, where each entry encapsulates information about a branch,
        /// including whether it was selected and its next beat ID
        /// </param>
        /// <returns>The beat ID of the next scene beat for the selected branch, or null if no branch was selected</returns>
        private string FindSelectedBranchTarget(List<BranchEvaluationResult> branchEvaluations)
        {
            for (int i = 0; i < branchEvaluations.Count; i++)
            {
                // Skip if the branch was not selected
                if (!branchEvaluations[i].WasSelected) continue;
                
                return branchEvaluations[i].NextBeatID;
            }

            return null;
        }

        /// <summary>
        /// Initiates the provided scene using the given cast of characters
        /// </summary>
        /// <param name="scene">The template representing the narrative scene to be started, including its structure and beats</param>
        /// <param name="cast">The collection of characters participating in the scene, indexed by their identifiers</param>
        /// <returns>A boolean value indicating whether the scene was successfully started</returns>
        public bool StartScene(ISceneTemplate scene, IReadOnlyDictionary<string, ICharacter> cast) => _inner.StartScene(scene, cast);

        /// <summary>
        /// Retrieves the current beat in the narrative flow of the active scene
        /// </summary>
        /// <returns>The current beat in the scene, or null if there is no current scene or the beat cannot be found</returns>
        public ISceneBeat GetCurrentBeat() => _inner.GetCurrentBeat();

        /// <summary>
        /// Retrieves the currently active scene template that is being orchestrated,
        /// capturing any relevant diagnostic data in the process
        /// </summary>
        /// <returns>
        /// An <c>ISceneTemplate</c> instance representing the currently active scene,
        /// or null if no scene is currently active
        /// </returns>
        public ISceneTemplate GetCurrentScene() => _inner.GetCurrentScene();

        /// <summary>
        /// Retrieves the current cast of characters associated with the active scene
        /// </summary>
        /// <returns>
        /// A read-only dictionary where keys represent character role identifiers,
        /// and values are the corresponding <c>ICharacter</c> instances
        /// </returns>
        public IReadOnlyDictionary<string, ICharacter> GetCurrentCast() => _inner.GetCurrentCast();

        /// <summary>
        /// Determines whether the specified beat has already been visited during the current scene
        /// </summary>
        /// <param name="beatID">The unique identifier of the beat to verify</param>
        /// <returns>True if the beat has been visited; otherwise, false</returns>
        public bool HasVisitedBeat(string beatID) => _inner.HasVisitedBeat(beatID);

        /// <summary>
        /// Checks whether a scene is currently active by querying the underlying SceneOrchestrator
        /// </summary>
        /// <returns>True if a scene is active; otherwise, false</returns>
        public bool ISceneActive() => _inner.IsSceneActive();

        /// <summary>
        /// Retrieves the character associated with the specified role identifier in the current scene's cast
        /// </summary>
        /// <param name="roleID">The identifier of the role whose character is to be retrieved</param>
        /// <returns>The character assigned to the given role identifier, or null if no character is assigned to the role</returns>
        public ICharacter GetCharacterForRole(string roleID) => _inner.GetCharacterForRole(roleID);

        /// <summary>
        /// Records the selection of a specific fragment within a scene beat, capturing its association
        /// with the beat identifier, fragment identifier, and speaker role identifier for diagnostic purposes
        /// </summary>
        /// <param name="beatID">The unique identifier of the beat where the fragment is selected</param>
        /// <param name="fragmentID">The unique identifier of the fragment being selected</param>
        /// <param name="speakerRoleID">The unique identifier of the speaker role associated with the selected fragment</param>
        public void RecordFragmentSelection(string beatID, string fragmentID, string speakerRoleID) => _inner.RecordFragmentSelection(beatID, fragmentID, speakerRoleID);
    }
}