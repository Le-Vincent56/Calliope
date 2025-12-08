using System;

namespace Calliope.Runtime.Diagnostics
{
    /// <summary>
    /// Interface for managing diagnostic mode
    /// </summary>
    public interface IDiagnosticsManager
    {
        /// <summary>
        /// Whether diagnostic mode is currently enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Enables diagnostic mode
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables diagnostic mode
        /// </summary>
        void Disable();

        /// <summary>
        /// Fired when diagnostic mode changes; parameter is the new enabled state
        /// </summary>
        event Action<bool> OnDiagnosticsModeChanged;
    }
}
