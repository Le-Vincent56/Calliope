using System.Collections;
using Calliope.Infrastructure.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Calliope.Unity.Components
{
    /// <summary>
    /// Simple dialogue presenter with fade in/out;
    /// Uses Unity's built-in animation system
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SimpleDialoguePresenter : BaseDialoguePresenter
    {
        [Header("UI References")]
        [Tooltip("Text component for the speaker name")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        
        [Tooltip("Text component for the dialogue text")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        
        [Tooltip("Optional continue button")]
        [SerializeField] private Button continueButton;
        
        [Header("Display Settings")]
        [Tooltip("Show dialogue panel on start")]
        [SerializeField] private bool showOnStart = true;
        
        [Tooltip("Fade duration (seconds)")]
        [SerializeField] private float fadeDuration = 0.2f;
        
        private CanvasGroup _canvasGroup;
        private bool _isShowing;

        public override bool IsShowing => _isShowing;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            // Hide initially unless showing on start
            if (!showOnStart)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // Exit case - no continue button configured
            if (!continueButton) return;
            
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        public override void PresentLine(LinePreparedEvent @event)
        {
            // Set speaker name
            if (speakerNameText)
                speakerNameText.text = @event.Speaker.DisplayName;
            
            // Set dialogue text
            if (dialogueText)
                dialogueText.text = @event.AssembledText;

            // Show the panel
            Show();
        }

        public override void Show()
        {
            // Exit case - already showing
            if (_isShowing) return;

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f));
            _isShowing = true;
        }

        public override void Hide()
        {
            if (!_isShowing) return;

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f));
            _isShowing = false;
        }

        public override void Clear()
        {
            if(speakerNameText)
                speakerNameText.text = string.Empty;
            
            if(dialogueText)
                dialogueText.text = string.Empty;
        }

        /// <summary>
        /// Handles the event triggered when the continue button is clicked;
        /// this method ensures proper scene orchestration by advancing to the next beat if available;
        /// if no further beats are present, the dialogue panel is hidden
        /// </summary>
        protected virtual void OnContinueClicked()
        {
            // Exit case - Calliope not initialized
            if (!Calliope.Instance) return;
            
            // Exit case - the scene is not active
            if (!Calliope.Instance.SceneOrchestrator.IsSceneActive()) return;

            // Check if there's another beat to advance to
            bool advanced = Calliope.Instance.SceneOrchestrator.AdvanceToNextBeat();

            // If advancing to another beat, do not hide the dialogue panel
            if (advanced) return;
            
            Hide();
        }

        /// <summary>
        /// Executes a fade animation for the dialogue panel, transitioning its alpha value over a duration
        /// to create a smooth fade-in or fade-out effect; this coroutine adjusts the visibility,
        /// interactivity, and raycasting properties of the panel based on the target alpha
        /// </summary>
        /// <param name="targetAlpha">
        /// The target alpha value to fade to, where 1.0 represents fully visible and 0.0 represents fully transparent
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerator"/> that represents the coroutine sequence for the fade animation
        /// </returns>
        private IEnumerator FadeRoutine(float targetAlpha)
        {
            // Get the starting variables
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            // Fade in/out for the duration
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            
            // Set final values
            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.interactable = targetAlpha > 0f;
            _canvasGroup.blocksRaycasts = targetAlpha > 0f;
        }
    }
}