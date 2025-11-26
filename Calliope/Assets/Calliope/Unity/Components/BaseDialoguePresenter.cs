using Calliope.Infrastructure.Events;
using Calliope.Runtime.Presentation;
using UnityEngine;

namespace Calliope.Unity.Components
{
    /// <summary>
    /// Abstract base class for presenting dialogues within a Unity-based system;
    /// handles subscription and unsubscription to dialogue-related events and defines
    /// core methods to control the visibility and content of dialogue UI components
    /// </summary>
    public abstract class BaseDialoguePresenter : MonoBehaviour, IDialoguePresenter
    {
        [Header("Base Settings")]
        [Tooltip("Automatically subscribe to dialogue events on enable")]
        [SerializeField] protected bool autoSubscribe = true;
        
        public abstract bool IsShowing { get; }

        protected virtual void OnEnable()
        {
            // Exit case - set not to auto-subscribe
            if (!autoSubscribe) return;
            
            // Exit case - Calliope not initialized
            if (!Calliope.Instance) return;
            
            Calliope.Instance.EventBus.Subscribe<LinePreparedEvent>(OnLinePrepared);
        }
        
        protected virtual void OnDisable()
        {
            // Exit case - set not to auto-subscribe
            if (!autoSubscribe) return;
            
            // Exit case - Calliope not initialized
            if (!Calliope.Instance) return;
            
            Calliope.Instance.EventBus.Unsubscribe<LinePreparedEvent>(OnLinePrepared);
        }

        /// <summary>
        /// Handles the event triggered when a dialogue line is prepared;
        /// this method is subscribed to the <see cref="LinePreparedEvent"/>
        /// and is responsible for invoking the presentation of the prepared dialogue line
        /// </summary>
        /// <param name="event">
        /// The <see cref="LinePreparedEvent"/> containing the data of the prepared dialogue line,
        /// including the speaker, target, assembled text, and selected fragment
        /// </param>
        private void OnLinePrepared(LinePreparedEvent @event) => PresentLine(@event);
        
        public abstract void PresentLine(LinePreparedEvent @event);
        public abstract void Show();
        public abstract void Hide();
        public abstract void Clear();
    }
}