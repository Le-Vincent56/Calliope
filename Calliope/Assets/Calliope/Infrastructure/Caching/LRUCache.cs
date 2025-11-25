using System;
using System.Collections.Generic;

namespace Calliope.Infrastructure.Caching
{
    /// <summary>
    /// Least Recently Used (LRU) cache with fixed capacity;
    /// O(1) for Get, Add, and Evict;
    /// Uses Dictionary and LinkedList for efficient operations
    /// </summary>
    public class LRUCache<TKey, TValue>
    {
        private class CacheItem
        {
            public TKey Key;
            public TValue Value;
        }
        
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;

        /// <summary>
        /// Gets the number of elements currently stored in the LRUCache
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Gets the maximum number of elements that the LRUCache can hold
        /// </summary>
        public int Capacity { get; }

        public LRUCache(int capacity)
        {
            // Validate capacity
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));

            Capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Attempts to retrieve a value associated with the specified key from the cache;
        /// if the key exists, the value is returned, and the key is marked as the most recently used
        /// </summary>
        /// <param name="key">The key of the element to retrieve</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the value parameter
        /// </param>
        /// <returns>True if the key is found in the cache; otherwise, false</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            // Check if the key exists in the cache
            if (_cache.TryGetValue(key, out LinkedListNode<CacheItem> node))
            {
                // Move the node to the front (the most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                
                // Return the cached value
                value = node.Value.Value;
                return true;
            }
            
            value = default;
            return false;
        }

        /// <summary>
        /// Adds a key-value pair to the cache; if the key already exists, updates its value
        /// and marks it as the most recently used; if the cache exceeds its capacity,
        /// evicts the least recently used item
        /// </summary>
        /// <param name="key">The key associated with the value to add or update</param>
        /// <param name="value">The value to associate with the specified key</param>
        public void Add(TKey key, TValue value)
        {
            // Check if the key already exists
            if (_cache.TryGetValue(key, out LinkedListNode<CacheItem> existingNode))
            {
                // Update the existing node
                existingNode.Value.Value = value;
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
            }
            else
            {
                // Add new
                if (_cache.Count == Capacity)
                {
                    // Evict the least recently used (last in the list)
                    LinkedListNode<CacheItem> lastNode = _lruList.Last;
                    _lruList.RemoveLast();
                    _cache.Remove(lastNode.Value.Key);
                }

                // Create the cache item
                CacheItem cacheItem = new CacheItem
                {
                    Key = key,
                    Value = value
                };
                
                // Add the node
                LinkedListNode<CacheItem> node = new LinkedListNode<CacheItem>(cacheItem);
                _lruList.AddFirst(node);
                _cache.Add(key, node);
            }
        }

        /// <summary>
        /// Retrieves the value associated with the specified key from the cache if it exists;
        /// if the key does not exist, a new value is generated using the provided factory function,
        /// added to the cache, and returned
        /// </summary>
        /// <param name="key">The key of the element to retrieve or add</param>
        /// <param name="factory">
        /// A function that generates a value for the specified key if the key is not already present in the cache
        /// </param>
        /// <returns>The value associated with the specified key, either retrieved or newly generated</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            // Retrieve the value if it exists
            if (TryGetValue(key, out TValue value))
                return value;

            // Construct the value through the provided factory
            value = factory.Invoke(key);
            
            // Add the value
            Add(key, value);
            return value;
        }

        /// <summary>
        /// Removes all entries from the cache, resetting it to its initial state
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }
}
