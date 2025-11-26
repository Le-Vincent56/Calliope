using Calliope.Infrastructure.Events;
using Calliope.Unity.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace Calliope.Samples.Presenters
{
    /// <summary>
    /// Handles the presentation of dialogue lines in a Unity-based application using PrimeTween,
    /// utilizing animations for visual effects such as scaling and fading.
    /// </summary>
    /// <remarks>
    /// This class extends the <see cref="BaseDialoguePresenter"/> to provide specialized functionality for presenting
    /// dialogue lines with optional typewriter effects, animating transitions, and handling user interactions
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public class PrimeTweenDialoguePresenter : BaseDialoguePresenter
    {
        [Header("UI References")]
        [Tooltip("Text component for the speaker name")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        
        [Tooltip("Text component for the dialogue text")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        
        [Tooltip("Optional continue button")]
        [SerializeField] private Button continueButton;
        
        [Header("Animation Settings")]
        [Tooltip("Duration of the fade-in animation (seconds)")]
        [SerializeField] private float fadeDuration = 0.3f;
        
        [Tooltip("Easing curve for fade")]
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        
        [Header("Animation Settings - Scale")]
        [Tooltip("Enable scale animation on show")]
        [SerializeField] private bool useScaleAnimation = true;
        
        [Tooltip("Starting scale for show animation (0.8 = 80% size)")]
        [SerializeField] [Range(0.1f, 1f)] private float scaleFrom = 0.8f;
        
        [Header("Animation Settings - Typewriter")]
        [Tooltip("Text typewriter effect speed in characters per second (0 = instant)")]
        [SerializeField] private float typewriterSpeed = 30f;
        
        [Tooltip("Easing for typewriter effect")]
        [SerializeField] private Ease typewriterEase = Ease.Linear;
        
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private bool _isShowing = false;
        private Sequence _currentSequence;
        private Tween _typewriterTween;
        private bool _isTypewriting = false;

        public override bool IsShowing => _isShowing;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            
            // Hide initially
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            // Exit case - no continue button assigned
            if (!continueButton) return;
            
            // Configure the continue button
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDestroy()
        {
            // Clean up tweens
            _currentSequence.Stop();
            _typewriterTween.Stop();
        }

        /// <summary>
        /// Presents a dialogue line by setting the speaker's name, applying the dialogue text with or without a typewriter effect,
        /// and displaying the dialogue panel, ensuring any ongoing typewriter animations are stopped and cleaned up before starting a new one
        /// </summary>
        /// <param name="event">The event containing the speaker's information and the prepared dialogue line to display</param>
        public override void PresentLine(LinePreparedEvent @event)
        {
            // Set the speaker name (instant)
            if(speakerNameText)
                speakerNameText.text = @event.Speaker.DisplayName;
            
            // Set dialogue text with an optional typewriter
            if (dialogueText)
            {
                // Stop any existing typewriter
                _typewriterTween.Stop();
                _isTypewriting = false;

                if (typewriterSpeed > 0f)
                {
                    // Tween a typewriter effect
                    dialogueText.text = @event.AssembledText;
                    dialogueText.maxVisibleCharacters = 0;
                    
                    // Calculate the duraiton based on the text length
                    int characterCount = @event.AssembledText.Length;
                    float duration = characterCount / typewriterSpeed;
                    
                    // Animate the character reveal
                    _isTypewriting = true;
                    _typewriterTween = Tween.Custom(0, characterCount, duration, value =>
                        {
                            // Exit case - there is no text component
                            if (dialogueText) return;
                            
                            dialogueText.maxVisibleCharacters = Mathf.RoundToInt(value);
                        }, 
                        ease: typewriterEase
                    ).OnComplete(() => _isTypewriting = false);
                }
                else
                {
                    // Instant text reveal
                    dialogueText.text = @event.AssembledText;
                    dialogueText.maxVisibleCharacters = int.MaxValue;
                }
            }

            // Show the panel
            Show();
        }

        /// <summary>
        /// Displays the dialogue presenter by initiating an animation sequence that combines a fade-in effect
        /// and optional scaling, depending on the configured settings; stops any existing animation processes
        /// before execution, ensuring a clean transition; updates the internal state to indicate the presenter
        /// is now visible and enables user interaction upon completion
        /// </summary>
        public override void Show()
        {
            // Exit case - already showing
            if (_isShowing) return;
            
            
            // Stop any existing animation
            _currentSequence.Stop();
            
            // Set to showing
            _isShowing = true;
            
            // Build animation sequence
            _currentSequence = Sequence.Create();

            // Check if using the scale animation
            if (useScaleAnimation)
            {
                // Start from the small scale
                _rectTransform.localScale = Vector3.one * scaleFrom;
                
                // Scale up
                _currentSequence.Group(Tween.Scale(
                    _rectTransform, 
                    Vector3.one, 
                    fadeDuration, 
                    fadeEase
                ));
            }
            
            // Fade in
            _currentSequence.Group(Tween.Alpha(
                _canvasGroup, 
                1f, 
                fadeDuration, 
                fadeEase
            ));
            
            // Enable interaction when done
            _currentSequence.OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            });
        }

        /// <summary>
        /// Hides the dialogue presenter and performs cleanup of the associated dialogue UI;
        /// stops any ongoing animations, including existing typewriter effects and fade animations,
        /// and resets the typewriter state to indicate no active typewriting process;
        /// disables the presenter's interaction, preventing user input and deactivating raycast behavior;
        /// animates the fade-out process with optional scaling if configured, and updates internal visibility state to hidden
        /// </summary>
        public override void Hide()
        {
            // Exit case - not showing
            if (!_isShowing) return;
            
            // Stop any existing animation
            _currentSequence.Stop();
            _typewriterTween.Stop();
            _isTypewriting = false;

            // Set to not showing
            _isShowing = false;
            
            // Disable interaction immediately
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            // Build hide sequence
            _currentSequence = Sequence.Create();
            
            // Fade out
            _currentSequence.Group(Tween.Alpha(
                _canvasGroup,
                0f, fadeDuration,
                fadeEase
            ));

            // Check if using scale animation
            if (useScaleAnimation)
            {
                // Scale down
                _currentSequence.Group(Tween.Scale(
                    _rectTransform, 
                    Vector3.one * scaleFrom, 
                    fadeDuration, 
                    fadeEase
                ));
            }
        }

        /// <summary>
        /// Clears all currently displayed dialogue information by resetting both
        /// the speaker name and dialogue text fields to empty strings;
        /// stops any ongoing animations, including any typewriter effects in progress
        /// resets the typewriter state flag to indicate no active typewriting process
        /// </summary>
        public override void Clear()
        {
            // Reset the speaker name text
            if(speakerNameText)
                speakerNameText.text = string.Empty;

            // Reset the dialogue text
            if (dialogueText)
            {
                dialogueText.text = string.Empty;
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
            
            // Stop any animations
            _currentSequence.Stop();
            _typewriterTween.Stop();
            _isTypewriting = false;
        }

        /// <summary>
        /// Handles the "continue" action triggered by the associated button;
        /// if a typewriter effect is in progress, skips it to display the full dialogue text;
        /// otherwise, attempts to advance the narrative to the next beat within the scene;
        /// if no further beats are available, hides the dialogue presenter
        /// </summary>
        protected virtual void OnContinueClicked()
        {
            // If the typewriter is active, skip it
            if (_isTypewriting && dialogueText)
            {
                // Show the full text
                _typewriterTween.Stop();
                dialogueText.maxVisibleCharacters = int.MaxValue;
                _isTypewriting = false;
                return;
            }

            // Exit cases - no Calliope instance or a scene is not active
            if (!Unity.Components.Calliope.Instance) return;
            if (!Unity.Components.Calliope.Instance.SceneOrchestrator.IsSceneActive()) return;
            
            // Advance to the next beat
            bool advanced = Unity.Components.Calliope.Instance.SceneOrchestrator.AdvanceToNextBeat();

            // Exit case - if a beat was successfully advanced to
            if (advanced) return;

            // Otherwise, there is nothing left to show in the scene, so hide the presenter
            Hide();
        }
    }
}