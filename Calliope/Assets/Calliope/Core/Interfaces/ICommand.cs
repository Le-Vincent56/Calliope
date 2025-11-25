namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Command pattern: encapsulates an action that can be executed and undone;
    /// used for relationship modifications with undo support
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Whether the command can be undone
        /// </summary>
        bool CanUndo { get; }
        
        /// <summary>
        /// Human-readable description for logging
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Execute the command
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo the command (restore previous state)
        /// </summary>
        void Undo();
    }
}