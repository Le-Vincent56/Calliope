using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Events
{
    /// <summary>
    /// Fired when a dialogue line has been selected and assembled;
    /// UI components listen to this to display dialogue
    /// </summary>
    public class LinePreparedEvent : CalliopeEventBase
    {
        /// <summary>
        /// The character speaking this line
        /// </summary>
        public ICharacter Speaker { get; }
        
        /// <summary>
        /// The character being spoken to (can be null)
        /// </summary>
        public ICharacter Target { get; }
        
        /// <summary>
        /// The final assembled text with variables substituted
        /// </summary>
        public string AssembledText { get; }
        
        /// <summary>
        /// The fragment that was selected
        /// </summary>
        public IDialogueFragment SelectedFragment { get; }

        public LinePreparedEvent(
            ICharacter speaker, 
            ICharacter target, 
            string assembledText,
            IDialogueFragment selectedFragment
        )
        {
            Speaker = speaker;
            Target = target;
            AssembledText = assembledText;
            SelectedFragment = selectedFragment;
        }
    }
}