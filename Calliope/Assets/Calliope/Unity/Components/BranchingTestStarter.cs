using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;
using UnityEngine;

namespace Calliope.Unity.Components
{
    /// <summary>
    /// Test wrapper for SceneStarter that allows manual cast assignment for
    /// testing branch logic
    /// </summary>
    public class BranchingTestStarter : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private SceneTemplateSO sceneTemplate;
        
        [Header("Manual Cast")]
        [SerializeField] private CharacterSO speakerCharacter;
        [SerializeField] private CharacterSO responderCharacter;
        
        [Header("Debug")]
        [SerializeField] private bool logBeatTransitions = true;

        private readonly StringBuilder _logBuilder = new StringBuilder();

        public void StartSceneWithManualCast()
        {
            Debug.Log("Starting with Manual Cast");
            
            Calliope calliope = Calliope.Instance;

            // Exit case - Calliope not initialized
            if (!calliope)
            {
                Debug.LogError("[BranchingTestStarter] Calliope not initialized");
                return;
            }
            
            // Validate inputs
            if (!ValidateInputs()) return;
            
            // Build manual cast
            Dictionary<string, ICharacter> cast = new Dictionary<string, ICharacter>
            {
                { "speaker", speakerCharacter },
                { "responder", responderCharacter }
            };
            
            Debug.Log("Set Cast");
            
            // Log cast
            _logBuilder.Clear();
            _logBuilder.Append("[BranchingTestStarter] Starting scene '");
            _logBuilder.Append(sceneTemplate.DisplayName);
            _logBuilder.Append("' with:\n  Speaker: ");
            _logBuilder.Append(speakerCharacter.DisplayName);
            _logBuilder.Append(" (traits: ");
            AppendTraitList(speakerCharacter.TraitIDs);
            _logBuilder.Append(")\n  Responder: ");
            _logBuilder.Append(responderCharacter.DisplayName);
            _logBuilder.Append(" (traits: ");
            AppendTraitList(responderCharacter.TraitIDs);
            _logBuilder.Append(")");
            Debug.Log(_logBuilder.ToString());
            
            // Start scene with manual cast
            if (!calliope.SceneOrchestrator.StartScene(sceneTemplate, cast))
            {
                Debug.LogError("[BranchingTestStarter] Failed to start scene");
                return;
            }

            // Present first beat
            PresentCurrentBeat(calliope);
        }
        
        private bool ValidateInputs()
        {
            _logBuilder.Clear();

            // Exit case - the scene template is not set
            if (!sceneTemplate)
            {
                _logBuilder.Append("[BranchingTestStarter] Scene template is not assigned");
                Debug.LogError(_logBuilder.ToString());
                return false;
            }

            // Exit case - the speaker character is not set
            if (!speakerCharacter)
            {
                _logBuilder.Append("[BranchingTestStarter] Speaker character is not assigned");
                Debug.LogError(_logBuilder.ToString());
                return false;
            }

            // Exit case - the responder character is not set
            if (!responderCharacter)
            {
                _logBuilder.Append("[BranchingTestStarter] Responder character is not assigned");
                Debug.LogError(_logBuilder.ToString());
                return false;
            }

            return true;
        }
        
        private void AppendTraitList(IReadOnlyList<string> traits) 
        {
            // Exit case - there are no traits to append
            if(traits == null || traits.Count == 0) 
            {
                _logBuilder.Append("none");
                return;
            }
            
            // Append each trait to the log
            for(int i = 0; i < traits.Count; i++) 
            {
                _logBuilder.Append(traits[i]);
                
                // Skip if at the last trait
                if(i >= traits.Count - 1) continue;
                
                _logBuilder.Append(", ");
            }
        }

        private void PresentCurrentBeat(Calliope calliope)
        {
            ISceneBeat beat = calliope.SceneOrchestrator.GetCurrentBeat();

            // Exit case - current beat is null
            if (beat == null)
            {
                Debug.LogError("[BranchingTestStarter] Current beat is null");
                return;
            }
            
            // Log beat transitions
            if (logBeatTransitions)
            {
                _logBuilder.Clear();
                _logBuilder.Append("[BranchingTestStarter] Beat: ");
                _logBuilder.Append(beat.BeatID);
                _logBuilder.Append(" | Speaker Role: ");
                _logBuilder.Append(beat.SpeakerRoleID);
                _logBuilder.Append(" | VariationSet: ");
                _logBuilder.Append(beat.VariationSetID);
                Debug.Log(_logBuilder.ToString());
            }
            
            // Get speaker and target
            ICharacter speaker = calliope.SceneOrchestrator.GetCharacterForRole(beat.SpeakerRoleID);
            ICharacter target = !string.IsNullOrEmpty(beat.TargetRoleID)
                ? calliope.SceneOrchestrator.GetCharacterForRole(beat.TargetRoleID)
                : null;
            
            // Get the variation set
            IVariationSet variationSet = calliope.VariationSetRepository.GetByID(beat.VariationSetID);

            // Exit case - variation set not found
            if (variationSet == null)
            {
                _logBuilder.Clear();
                _logBuilder.Append("[BranchingTestStarter] VariationSet '");
                _logBuilder.Append(beat.VariationSetID);
                _logBuilder.Append("' not found");
                Debug.LogError(_logBuilder.ToString());
                return;
            }
            
            // Build and present the line
            calliope.DialogueLineBuilder.BuildLine(
                variationSet.Variations,
                speaker,
                target,
                applyRelationshipModifiers: true
            );
        }

        public void ContinueScene()
        {
            Calliope calliope = Calliope.Instance;

            // Exit case - Calliope not initialized
            if (!calliope)
            {
                Debug.LogError("BranchingTestStarter] Calliope not initialized");
                return;
            }

            // Exit case - the scene has ended
            if (!calliope.SceneOrchestrator.AdvanceToNextBeat())
            {
                Debug.Log("[BranchingTestStarter] No more beats to advance to; ending scene");
                return;
            }
            
            PresentCurrentBeat(calliope);
        }

        public void RunFullScene()
        {
            Debug.Log("Called RunFullScene()");
            
            Calliope calliope = Calliope.Instance;
            
            // Exit case - Calliope not initialized
            if (!calliope)
            {
                Debug.LogError("[BranchingTestStarter] Calliope not initialized");
                return;
            }
            
            // Start the scene
            StartSceneWithManualCast();
            
            // Track visited beats
            List<string> visitedBeats = new List<string>();
            int maxIterations = 100;
            int iterations = 0;

            // Run the scene until the end
            while (calliope.SceneOrchestrator.IsSceneActive() && iterations < maxIterations)
            {
                ISceneBeat currentBeat = calliope.SceneOrchestrator.GetCurrentBeat();
                
                // Track the current beat
                if(currentBeat != null)
                    visitedBeats.Add(currentBeat.BeatID);
                
                // Exit case - reached the end of the scene
                if(!calliope.SceneOrchestrator.AdvanceToNextBeat()) break;
                
                // Present the current beat
                PresentCurrentBeat(calliope);
                iterations++;
            }
            
            Debug.Log("Ended Iteration");
            
            // Log the summary
            _logBuilder.Clear();
            _logBuilder.Append("[BranchingTestStarter] Scene complete. Path taken:\n");

            for (int i = 0; i < visitedBeats.Count; i++)
            {
                _logBuilder.Append(visitedBeats[i]);

                // Skip if at the last beat
                if (i >= visitedBeats.Count - 1) continue;
                
                _logBuilder.Append(" → ");
            }
            
            Debug.Log(_logBuilder.ToString());
            
            // Verify expected path
            VerifyExpectedPath(visitedBeats);
        }
        
        private void VerifyExpectedPath(List<string> visitedBeats)
        {
            // Determine expected response based on responder traits
            string expectedResponse;

            if (responderCharacter.HasTrait("brave")) expectedResponse = "brave-response";
            else if (responderCharacter.HasTrait("kind")) expectedResponse = "kind-response";
            else expectedResponse = "cynical-response";
            
            // Check which response was actually taken
            string actualResponse = "none";

            for (int i = 0; i < visitedBeats.Count; i++)
            {
                string beatID = visitedBeats[i];

                // Exit case - no response was taken
                if (beatID != "brave-response" && beatID != "kind-response" && beatID != "cynical-response") continue;
                
                actualResponse = beatID;
                break;
            }
            
            // Log verification result
            _logBuilder.Clear();
            _logBuilder.Append("[BranchingTestStarter] === VERIFICATION ===\n");
            _logBuilder.Append("\tResponder: ");
            _logBuilder.Append(responderCharacter.DisplayName);
            _logBuilder.Append("\t(traits: ");
            AppendTraitList(responderCharacter.TraitIDs);
            _logBuilder.Append(")\n \tExpected: ");
            _logBuilder.Append(expectedResponse);
            _logBuilder.Append("\n \tActual: ");
            _logBuilder.Append(actualResponse);
            _logBuilder.Append("\n \tResult: ");

            // Log result
            if (expectedResponse == actualResponse)
            {
                _logBuilder.Append("Passed");
                Debug.Log(_logBuilder.ToString());
            }
            else
            {
                _logBuilder.Append("Failed");
                Debug.LogError(_logBuilder.ToString());
            }
        }
    }
}
