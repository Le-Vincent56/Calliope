using Calliope.Infrastructure.Events;

namespace Calliope.Runtime.Presentation
{
    /// <summary>
    /// Interface for dialogue presenters;
    /// allows custom UI systems to handle dialogue display
    /// </summary>
    public interface IDialoguePresenter
    {
        /// <summary>
        /// Check if the PResenter is currently showing dialogue
        /// </summary>
        bool IsShowing { get; }

        /// <summary>
        /// Presents a prepared dialogue line to the user interface
        /// </summary>
        /// <param name="event">
        /// The prepared dialogue line event containing information about
        /// the speaker, target, assembled text, and the selected fragment
        /// </param>
        void PresentLine(LinePreparedEvent @event);

        /// <summary>
        /// Show the dialogue UI
        /// </summary>
        void Show();

        /// <summary>
        /// Hide the dialogue UI
        /// </summary>
        void Hide();

        /// <summary>
        /// Clear the current dialogue display
        /// </summary>
        void Clear();
    }
}
