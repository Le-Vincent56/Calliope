using System;

namespace Calliope.Infrastructure.Events
{
    /// <summary>
    /// The central event bus for decoupled communication;
    /// publishers fire events and Subscribers react to them
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to the event bus, allowing subscribers to react to it
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to be published</typeparam>
        /// <param name="event">The event instance to be published</param>
        void Publish<TEvent>(TEvent @event) where TEvent : class;

        /// <summary>
        /// Subscribes to a specific event type, providing a handler that will be executed when the event is published
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to subscribe to</typeparam>
        /// <param name="handler">The action to execute when the event is triggered</param>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Unsubscribes a handler from a specific event type, preventing it from being executed when the event is published
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to unsubscribe from</typeparam>
        /// <param name="handler">The action that was previously subscribed and is to be removed</param>
        /// <remarks>Important: Call this in OnDestroy or OnDisable to prevent memory leaks</remarks>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    }
}