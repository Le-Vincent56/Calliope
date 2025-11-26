using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using Calliope.Infrastructure.Logging;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Orchestrates scene flow: beat progression, branching logic, and scene completion;
    /// maintains the current scene state and determines next beats based on conditions
    /// </summary>
    public class SceneOrchestrator
    {
        private readonly IRelationshipProvider _relationshipProvider;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        private ISceneTemplate _currentScene;
        private IReadOnlyDictionary<string, ICharacter> _currentCast;
        private string _currentBeatID;
        private HashSet<string> _visitedBeats;

        public SceneOrchestrator(IRelationshipProvider relationshipProvider, IEventBus eventBus, ILogger logger)
        {
            _relationshipProvider = relationshipProvider ?? throw new ArgumentNullException(nameof(relationshipProvider));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _visitedBeats = new HashSet<string>();
        }

        /// <summary>
        /// Initiates the given scene using the provided cast of characters;
        /// if the scene or cast is invalid, or if the scene lacks a valid starting beat, an error is logged and the process terminates
        /// </summary>
        /// <param name="scene">The scene template to be orchestrated, containing the structure and beats of the scene</param>
        /// <param name="cast">The collection of characters participating in the scene, identified by unique keys</param>
        /// <returns>
        /// True if the scene is successfully started; otherwise, false if the scene, cast, or starting beat is invalid or missing
        /// </returns>
        public bool StartScene(ISceneTemplate scene, IReadOnlyDictionary<string, ICharacter> cast)
        {
            // Exit case - there is no scene to orchestrate
            if (scene == null)
            {
                _logger.LogError("[SceneOrchestrator] Cannot start scene: scene is null");
                return false;
            }

            // Exit case - there is no cast to use for the scene
            if (cast == null || cast.Count == 0)
            {
                _logger.LogError("[SceneOrchestrator] Cannot start scene: cast is empty");
                return false;
            }
            
            StringBuilder logBuilder = new StringBuilder();

            // Exit case - the scene has no starting beat
            if (string.IsNullOrEmpty(scene.StartingBeatID))
            {
                // Build the error message
                logBuilder.Append("[SceneOrchestrator] Cannot start scene '");
                logBuilder.Append(scene.ID);
                logBuilder.Append("': no start beat specified");
                
                _logger.LogError(logBuilder.ToString());
                return false;
            }

            // Exit case - the starting beat does not exist in the scene's beats
            if (!scene.Beats.ContainsKey(scene.StartingBeatID))
            {
                // Build the error message
                logBuilder.Append("[SceneOrchestrator] Cannot start scene '");
                logBuilder.Append(scene.ID);
                logBuilder.Append("': start beat '");
                logBuilder.Append(scene.StartingBeatID);
                logBuilder.Append("' does not exist in the scene's beats");
                
                _logger.LogError(logBuilder.ToString());
                return false;
            }
            
            // Initialize the scene state
            _currentScene = scene;
            _currentCast = cast;
            _currentBeatID = scene.StartingBeatID;
            _visitedBeats.Clear();
            _visitedBeats.Add(_currentBeatID);
            
            // Build the log message
            logBuilder.Append("[SceneOrchestrator] Started scene '");
            logBuilder.Append(scene.ID);
            logBuilder.Append("' with starting beat '");
            logBuilder.Append(scene.StartingBeatID);
            logBuilder.Append("'");
            
            _logger.LogInfo(logBuilder.ToString());
            
            // Publish the scene start event
            _eventBus.Publish(new SceneCastEvent(scene.ID, cast));
            
            return true;
        }

        /// <summary>
        /// Retrieves the current beat in the ongoing scene based on the current beat ID;
        /// if the current beat ID or scene is not set or valid, an error is logged and null is returned
        /// </summary>
        /// <returns>
        /// The current beat object if successfully found; otherwise, null if the beat ID
        /// is invalid, absent, or does not exist in the current scene's beat collection
        /// </returns>
        public ISceneBeat GetCurrentBeat()
        {
            // Exit case - there is no current scene
            if (_currentScene == null || string.IsNullOrEmpty(_currentBeatID))
            {
                _logger.LogError("[SceneOrchestrator] No current scene or beat");
                return null;
            }

            // Exit case - the current beat exists in the scene's beats
            if (_currentScene.Beats.TryGetValue(_currentBeatID, out ISceneBeat beat)) return beat;
            
            StringBuilder errorBuilder = new StringBuilder();
                
            // Build the error message
            errorBuilder.Append("[SceneOrchestrator] Current beat '");
            errorBuilder.Append(_currentBeatID);
            errorBuilder.Append("' not found in the current scene");
                
            _logger.LogError(errorBuilder.ToString());
            return null;
        }

        /// <summary>
        /// Advances the scene to the next beat by evaluating the branches of the current beat
        /// and determining the appropriate next step; if there are no valid branches, the scene
        /// is concluded, and the operation ends
        /// </summary>
        /// <returns>
        /// A boolean value indicating whether the operation successfully advanced to the next beat;
        /// returns false if the current beat is null, if there are no branches, or if no valid branch
        /// is found to transition to
        /// </returns>
        public bool AdvanceToNextBeat()
        {
            ISceneBeat currentBeat = GetCurrentBeat();
            
            // Exit case - there is no current beat
            if (currentBeat == null)
            {
                _logger.LogError("[SceneOrchestrator] Cannot advance: no current beat");
                return false;
            }
            
            StringBuilder logBuilder = new StringBuilder();
            
            // Exit case - there are no branches to advance to
            if (currentBeat.Branches == null || currentBeat.Branches.Count == 0)
            {
                // No branches means the scene is complete; build the log message
                logBuilder.Append("[SceneOrchestrator] Scene '");
                logBuilder.Append(_currentScene.ID);
                logBuilder.Append("' complete at beat '");
                logBuilder.Append(_currentBeatID);
                logBuilder.Append("'");
                _logger.LogInfo(logBuilder.ToString());
                
                // End the scene
                EndScene();
                return false;
            }

            // Evaluate what the next beat should be
            string nextBeatID = EvaluateBranches(currentBeat.Branches);

            // Exit case - no valid branch was found
            if (string.IsNullOrEmpty(nextBeatID))
            {
                logBuilder.Append("[SceneOrchestrator] No valid branch found from beat '");
                logBuilder.Append(_currentBeatID);
                logBuilder.Append("', ending scene");
                _logger.LogInfo(logBuilder.ToString());
                
                EndScene();
                return false;
            }
            
            // Transition to the next beat
            string previousBeatID = _currentBeatID;
            _currentBeatID = nextBeatID;
            _visitedBeats.Add(_currentBeatID);
            
            // Build the log message
            logBuilder.Append("[SceneOrchestrator] Advanced from beat '");
            logBuilder.Append(previousBeatID);
            logBuilder.Append("' to beat '");
            logBuilder.Append(_currentBeatID);
            logBuilder.Append("'");
            
            _logger.LogInfo(logBuilder.ToString());
            return true;
        }

        /// <summary>
        /// Evaluates the provided list of beat branches to determine the next beat ID to transition to,
        /// based on the conditions defined for each branch
        /// </summary>
        /// <param name="branches">
        /// A read-only list of <c>IBeatBranch</c> instances representing the possible branches associated
        /// with the current beat; each branch may define conditions that must be satisfied for the branch to be selected
        /// </param>
        /// <returns>
        /// The identifier of the next beat to transition to if a valid branch is found;
        /// returns null if no branch conditions are satisfied or if no branches are provided
        /// </returns>
        private string EvaluateBranches(IReadOnlyList<IBeatBranch> branches)
        {
            StringBuilder logBuilder = new StringBuilder();
            
            // Evaluate each branch in order
            for (int i = 0; i < branches.Count; i++)
            {
                IBeatBranch branch = branches[i];
                
                // Exit case - there are no conditions; the branch always succeeds (fallback)
                if (branch.Conditions == null || branch.Conditions.Count == 0)
                {
                    logBuilder.Clear();
                    logBuilder.Append("[SceneOrchestrator] Taking unconditional branch to '");
                    logBuilder.Append(branch.NextBeatID);
                    logBuilder.Append("'");
                    
                    _logger.LogInfo(logBuilder.ToString());
                    return branch.NextBeatID;
                }
                
                // Check if all conditions are met
                bool allConditionsMet = true;
                for (int j = 0; j < branch.Conditions.Count; j++)
                {
                    IBranchCondition condition = branch.Conditions[j];
                    
                    // Skip if the condition is met
                    if (condition.Evaluate(_currentCast, _relationshipProvider)) continue;

                    // Notify that a condition was not met
                    allConditionsMet = false;
                    break;
                }

                // Skip if not all conditions were met
                if (!allConditionsMet) continue;
                
                logBuilder.Clear();
                logBuilder.Append("[SceneOrchestrator] Taking conditional branch to '");
                logBuilder.Append(branch.NextBeatID);
                logBuilder.Append("' (all conditions met)");
                    
                _logger.LogInfo(logBuilder.ToString());
                return branch.NextBeatID;
            }
            
            // No branches matched
            _logger.LogWarning("[SceneOrchestrator] No branches matched their conditions");
            return null;
        }

        /// <summary>
        /// Retrieves the character associated with the specified role ID within the current scene cast
        /// </summary>
        /// <param name="roleID">
        /// The identifier of the role for which to retrieve the associated character
        /// </param>
        /// <returns>
        /// The <c>ICharacter</c> instance that corresponds to the specified role ID;
        /// returns null if no character is assigned to the role or if the cast is not initialized
        /// </returns>
        public ICharacter GetCharacterForRole(string roleID) => _currentCast?.GetValueOrDefault(roleID);

        /// <summary>
        /// Retrieves the currently active scene template being orchestrated
        /// </summary>
        /// <returns>
        /// The <c>ISceneTemplate</c> instance representing the currently active scene;
        /// returns null if no scene is currently active
        /// </returns>
        public ISceneTemplate GetCurrentScene() => _currentScene;

        /// <summary>
        /// Retrieves the current cast of characters associated with the active scene
        /// </summary>
        /// <returns>
        /// A read-only dictionary where the key is the character's role identifier,
        /// and the value is the corresponding <c>ICharacter</c> instance
        /// </returns>
        public IReadOnlyDictionary<string, ICharacter> GetCurrentCast() => _currentCast;

        /// <summary>
        /// Determines whether the specified beat has already been visited in the current scene
        /// </summary>
        /// <param name="beatID">The unique identifier of the beat to check.</param>
        /// <returns>True if the beat with the specified identifier has been visited; otherwise, false</returns>
        public bool HasVisitedBeat(string beatID) => _visitedBeats.Contains(beatID);

        /// <summary>
        /// Retrieves a collection of beat identifiers that have been visited during the current scene
        /// </summary>
        /// <returns>A read-only collection of strings representing the visited beats</returns>
        public IReadOnlyCollection<string> GetVisitedBeats() => _visitedBeats;

        /// <summary>
        /// Determines whether a scene is currently active by checking if the current scene is set
        /// </summary>
        /// <returns>True if a scene is currently active; otherwise, false</returns>
        public bool IsSceneActive() => _currentScene != null;

        /// <summary>
        /// Ends the currently active scene and resets all the related state in the orchestrator
        /// </summary>
        private void EndScene()
        {
            if (_currentScene != null)
            {
                StringBuilder logBuilder = new StringBuilder();
                logBuilder.Append("[SceneOrchestrator] Ending scene '");
                logBuilder.Append(_currentScene.ID);
                logBuilder.Append("'");
                
                _logger.LogInfo(logBuilder.ToString());
            }
            
            // Reset values
            _currentScene = null;
            _currentCast = null;
            _currentBeatID = null;
            _visitedBeats.Clear();
        }
    }
}