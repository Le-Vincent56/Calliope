using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.Components
{
    /// <summary>
    /// Simple helper to start a Calliope scene;
    /// attach a button or call StartScene() from code
    /// </summary>
    public class SceneStarter : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [Tooltip("ID of the scene template to start")]
        [SerializeField] private string sceneTemplateID;

        /// <summary>
        /// Initiates the process of starting a scene by resolving and casting the required characters
        /// into the roles specified by the scene template; it also handles errors related to missing
        /// templates, failed casting, and uninitialized dependencies; upon successful setup, the method
        /// delegates further scene execution to the Scene Orchestrator and prepares the scene for progression
        /// </summary>
        public void StartScene()
        {
            Calliope calliope = Calliope.Instance;
            if (!calliope)
            {
                Debug.LogError("[SceneStarter] Calliope not initialized");
                return;
            }
            
            // Get the scene template
            ISceneTemplate sceneTemplate = calliope.SceneTemplateRepository.GetByID(sceneTemplateID);
            
            StringBuilder errorBuilder = new StringBuilder();
            
            // Exit case - if the scene template is not found
            if (sceneTemplate == null)
            {
                errorBuilder.Append("[SceneStarter] Scene template '");
                errorBuilder.Append(sceneTemplateID);
                errorBuilder.Append("' not found");
                
                Debug.LogError(errorBuilder.ToString());
                return;
            }
            
            // Get all the characters
            IReadOnlyList<ICharacter> characters = calliope.CharacterRepository.GetAll();
            
            // Cast the scene
            IReadOnlyDictionary<string, ICharacter> cast = calliope.CharacterCaster.CastScene(sceneTemplate.Roles, characters);

            // Exit case - casting the scene failed
            if (cast == null)
            {
                errorBuilder.Append("[SceneStarter] Failed to cast scene '");
                errorBuilder.Append(sceneTemplateID);
                errorBuilder.Append("'");
                
                Debug.LogError(errorBuilder.ToString());
                return;
            }
            
            // Start scene
            calliope.SceneOrchestrator.StartScene(sceneTemplate, cast);
            
            // Present the first beat
            PresentCurrentBeat();
        }

        /// <summary>
        /// Processes and presents the current beat of the scene by resolving key elements such as the speaker,
        /// the target, and the associated dialogue variations; it retrieves the necessary details for the beat
        /// from the scene orchestrator and constructs the dialogue line, applying relationship modifiers where applicable
        /// </summary>
        private void PresentCurrentBeat()
        {
            // Get the Calliope instance
            Calliope calliope = Calliope.Instance;
            
            // Get the current beat
            ISceneBeat beat = calliope.SceneOrchestrator.GetCurrentBeat();

            // Get the speaker and target characters
            ICharacter speaker = calliope.SceneOrchestrator.GetCharacterForRole(beat.SpeakerRoleID);
            ICharacter target = !string.IsNullOrEmpty(beat.TargetRoleID) 
                ? calliope.SceneOrchestrator.GetCharacterForRole(beat.TargetRoleID) 
                : null;

            // Get the variation set
            IVariationSet variationSet = calliope.VariationSetRepository.GetByID(beat.VariationSetID);

            calliope.DialogueLineBuilder.BuildLine(
                variationSet.Variations,
                speaker,
                target,
                applyRelationshipModifiers: true
            );
        }

        /// <summary>
        /// Advances the scene to the next beat and handles its presentation;
        /// this method works in conjunction with the Calliope scene framework;
        /// if there are no further beats to advance to or if the next beat fails to load,
        /// the method will exit without further action
        /// </summary>
        public void ContinueScene()
        {
            Calliope calliope = Calliope.Instance;

            // Exit case - there is no beat to advance to (end of scene or failed to get a beat)
            if (!calliope.SceneOrchestrator.AdvanceToNextBeat()) return;
            
            // Present the next beat
            PresentCurrentBeat();
        }
    }
}