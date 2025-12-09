using System;
using Calliope.Infrastructure.Events;

namespace Calliope.Runtime.Diagnostics
{
    /// <summary>
    /// Singleton manager for diagnostic mode;
    /// when enabled, decorators will capture and publish detailed events
    /// </summary>
    public class DiagnosticsManager : IDiagnosticsManager
    {
        private static DiagnosticsManager _instance;
        public static DiagnosticsManager Instance => _instance ??= new DiagnosticsManager();

        private bool _isEnabled;
        private IEventBus _eventBus;
        
        public bool IsEnabled => _isEnabled;
        public IEventBus EventBus => _eventBus;

        public event Action<bool> OnDiagnosticsModeChanged;

        private DiagnosticsManager() => _isEnabled = false;

        /// <summary>
        /// Initializes the DiagnosticsManager with the specified event bus
        /// </summary>
        /// <param name="eventBus">An instance of <see cref="IEventBus"/> used for handling diagnostic events</param>
        public void Initialize(IEventBus eventBus) => _eventBus = eventBus;

        /// <summary>
        /// Enables diagnostic mode, allowing decorators to capture and publish detailed events
        /// </summary>
        public void Enable()
        {
            // Exit case - already enabled
            if (_isEnabled) return;
            
            _isEnabled = true;
            OnDiagnosticsModeChanged?.Invoke(true);
        }

        /// <summary>
        /// Disables diagnostic mode, stopping the capture and publishing of detailed events
        /// </summary>
        public void Disable()
        {
            if (!_isEnabled) return;
            
            _isEnabled = false;
            OnDiagnosticsModeChanged?.Invoke(false);
        }

        /// <summary>
        /// Resets the DiagnosticsManager instance, clearing its state and releasing any references to the event bus
        /// </summary>
        public static void Reset()
        {
            if (_instance != null)
            {
                // Reset the instance
                _instance._isEnabled = false;
                _instance._eventBus = null;
                _instance.OnDiagnosticsModeChanged = null;
            }

            _instance = null;
        }
    }
}