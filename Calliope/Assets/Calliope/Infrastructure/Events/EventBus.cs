using System;
using System.Collections.Generic;
using System.Text;

namespace Calliope.Infrastructure.Events
{
    /// <summary>
    /// A simple, synchronous event bus implementation;
    /// thread-safe for Unity's single-threaded model
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        /// <summary>
        /// Publishes an event to all subscribed handlers, invoking each handler with the provided event data
        /// </summary>
        /// <typeparam name="TEvent">The type of the event being published</typeparam>
        /// <param name="event">The instance of the event to be published; cannot be null</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided event instance is null</exception>
        public void Publish<TEvent>(TEvent @event) where TEvent : class
        {
            // Exit case - no event is given to publish
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            List<Delegate> handlers;
            
            // Provide thread safety by locking
            lock (_lock)
            {
                // Exit case - there are no subscribers for the provided event type
                if (!_subscribers.TryGetValue(typeof(TEvent), out handlers)) return;
                
                // Copy to avoid modification during iteration
                handlers = new List<Delegate>(handlers);
            }
            
            // Invoke all handlers
            for (int i = 0; i < handlers.Count; i++)
            {
                try
                {
                    // Try to cast and invoke the handler
                    ((Action<TEvent>)handlers[i]).Invoke(@event);
                }
                catch (Exception ex)
                {
                    // Build the exception
                    StringBuilder exceptionBuilder = new StringBuilder();
                    exceptionBuilder.Append("[EventBus] Handler error for ");
                    exceptionBuilder.Append(typeof(TEvent).Name);
                    exceptionBuilder.Append(": ");
                    exceptionBuilder.Append(ex);
                    
                    // Log the exception
                    UnityEngine.Debug.LogError(exceptionBuilder.ToString());
                }
            }
        }

        /// <summary>
        /// Subscribes a handler to listen for events of the specified type
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to subscribe to</typeparam>
        /// <param name="handler">The action to execute when an event of type <typeparamref name="TEvent"/> is published</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="handler"/> is null</exception>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            // Exit case - no handler is given as a subscriber
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Provide thread safety by locking
            lock (_lock)
            {
                // Extract the event type
                Type eventType = typeof(TEvent);

                // If the event type does not exist yet, create a new list of subscribers
                if (!_subscribers.ContainsKey(eventType)) 
                    _subscribers[eventType] = new List<Delegate>();
                
                // Add the handler to the list of subscribers
                _subscribers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribes a handler from receiving notifications for a specific event type
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to unsubscribe from</typeparam>
        /// <param name="handler">The handler to be removed from the subscriber list for the specified event type</param>
        /// <exception cref="ArgumentNullException">Thrown if the handler is null</exception>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            // Exit case - no handler is given as a subscriber
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Provide thread safety by locking
            lock (_lock)
            {
                // Extract the event type
                Type eventType = typeof(TEvent);

                // Exit case - there are no subscribers for the provided event type
                if (!_subscribers.TryGetValue(eventType, out List<Delegate> handlers)) return;
                
                // Remove the handler from the list of subscribers
                handlers.Remove(handler);

                // Remove the event type from the dictionary if there are no more subscribers
                if (handlers.Count == 0) _subscribers.Remove(eventType);
            }
        }
    }
}