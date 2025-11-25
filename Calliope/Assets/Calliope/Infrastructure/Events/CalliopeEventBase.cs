using System;

namespace Calliope.Infrastructure.Events
{
    /// <summary>
    /// The base class for all Calliope events;
    /// provides a timestamp for debugging/logging
    /// </summary>
    public abstract class CalliopeEventBase
    {
        /// <summary>
        /// When this event was created (UTC)
        /// </summary>
        public DateTime Timestamp { get; }
        
        protected CalliopeEventBase() => Timestamp = DateTime.UtcNow;
    }
}