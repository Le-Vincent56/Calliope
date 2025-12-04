using System;
using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime
{
    /// <summary>
    /// Generic key-value store for the scene state;
    /// thread-safe implementation supporting any data type
    /// </summary>
    public class SceneContext : ISceneContext
    {
        private readonly Dictionary<string, object> _values;
        private readonly object _lock = new object();

        private const string BeatVisitedFormat = "beat.{0}.visited";
        private const string BeatFragmentFormat = "beat.{0}.fragment";
        private const string BeatSpeakerFormat = "beat.{0}.speaker";

        public SceneContext()
        {
            _values = new Dictionary<string, object>();
        }

        /// <summary>
        /// Sets the value for a given key in the scene context; this method is thread-safe
        /// and ensures the value is stored correctly in the underlying data structure
        /// </summary>
        /// <param name="key">The unique identifier for the value to be set; this must not be null or empty</param>
        /// <param name="value">The value to be associated with the specified key; the value can be of any object type</param>
        public void SetValue(string key, object value)
        {
            // Exit case - the key is invalid
            if (string.IsNullOrEmpty(key)) return;

            // Lock for thread-safety
            lock (_lock)
            {
                _values[key] = value;
            }
        }
        
        public T GetValue<T>(string key, T defaultValue = default) 
        {
            // Exit case - the key is invalid
            if (string.IsNullOrEmpty(key)) return defaultValue;

            // Lock for thread-safety
            lock (_lock)
            {
                // Exit case - the key is not found
                if (!_values.TryGetValue(key, out object value))
                    return defaultValue;

                // Exit case - the value is already of the correct type
                if (value is T typedValue)
                    return typedValue;

                // Attempt conversion for numeric types
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// Determines whether a specified key exists in the scene context;
        /// this method is thread-safe and ensures that key existence is checked
        /// accurately within the underlying data structure
        /// </summary>
        /// <param name="key">The key to check for existence in the context; this must not be null or empty</param>
        /// <returns>True if the key exists in the context; otherwise, false</returns>
        public bool HasKey(string key)
        {
            // Exit case - the key is invalid
            if (string.IsNullOrEmpty(key)) return false;

            // Lock for thread-safety
            lock (_lock)
            {
                return _values.ContainsKey(key);
            }
        }

        /// <summary>
        /// Removes the value associated with the specified key from the scene context;
        /// this method is thread-safe and ensures the key-value pair is removed correctly
        /// from the underlying data structure
        /// </summary>
        /// <param name="key">The unique identifier of the key to be removed; this must not be null or empty</param>
        /// <returns>True if the key was successfully removed; false if the key does not exist or is invalid</returns>
        public bool RemoveKey(string key)
        {
            // Exit case - the key is invalid
            if (string.IsNullOrEmpty(key)) return false;

            // Lock for thread-safety
            lock (_lock)
            {
                return _values.Remove(key);
            }
        }

        /// <summary>
        /// Retrieves all keys currently stored in the scene context;
        /// this method is thread-safe and ensures consistent access to the key collection
        /// </summary>
        /// <returns>A collection of strings representing all keys in the scene context</returns>
        public IEnumerable<string> GetAllKeys()
        {
            // Lock for thread-safety
            lock (_lock)
            {
                return new List<string>(_values.Keys);
            }
        }

        /// <summary>
        /// Retrieves a collection of keys from the scene context that begin with the specified prefix;
        /// this method ensures thread-safe access to the underlying key-value store
        /// </summary>
        /// <param name="prefix">
        /// The prefix to filter keys; only keys starting with this string will be included
        /// if null or empty, all keys will be returned
        /// </param>
        /// <returns>A collection of keys that start with the specified prefix; if no keys match, an empty collection is returned</returns>
        public IEnumerable<string> GetKeysWithPrefix(string prefix)
        {
            // Exit case - the prefix is invalid
            if (string.IsNullOrEmpty(prefix)) return GetAllKeys();

            List<string> result = new List<string>();
            
            // Lock for thread-safety
            lock (_lock)
            {
                foreach (string key in _values.Keys)
                {
                    // Skip if the string does not start with the prefix
                    if (!key.StartsWith(prefix)) continue;
                    
                    result.Add(key);
                }
            }

            return result;
        }

        /// <summary>
        /// Clears all key-value pairs stored within the scene context; this method is thread-safe
        /// and ensures the underlying data structure is emptied without interference from concurrent operations
        /// </summary>
        public void Clear()
        {
            // Lock for thread-safety
            lock (_lock)
            {
                _values.Clear();
            }
        }

        public void Increment(string key, float incrementBy = 1f)
        {
            // Exit case - the key is invalid
            if (string.IsNullOrEmpty(key)) return;

            // Lock for thread-safety
            lock (_lock)
            {
                float currentValue = 0f;
                
                // Attempt to retrieve the existing value
                if (_values.TryGetValue(key, out object existing))
                {
                    try
                    {
                        currentValue = Convert.ToSingle(existing);
                    }
                    catch
                    {
                        currentValue = 0f;
                    }
                }
                
                _values[key] = currentValue + incrementBy;
            }
        }

        /// <summary>
        /// Records a visit to a specific beat in the scene context and optionally associates it with a fragment and a speaker role
        /// </summary>
        /// <param name="beatID">The unique identifier for the beat being visited; this must not be null or empty</param>
        /// <param name="fragmentID">An optional identifier for the fragment associated with the beat; if null or empty, no fragment will be recorded</param>
        /// <param name="speakerRoleID">An optional identifier for the speaker role associated with the beat; if null or empty, no speaker role will be recorded</param>
        public void RecordBeatVisit(string beatID, string fragmentID, string speakerRoleID)
        {
            if (string.IsNullOrEmpty(beatID)) return;

            SetValue(string.Format(BeatVisitedFormat, beatID), true);

            if (!string.IsNullOrEmpty(fragmentID))
                SetValue(string.Format(BeatFragmentFormat, beatID), fragmentID);

            if (!string.IsNullOrEmpty(speakerRoleID))
                SetValue(string.Format(BeatSpeakerFormat, beatID), speakerRoleID);
        }

        /// <summary>
        /// Determines whether a specific beat has been marked as visited in the scene context
        /// </summary>
        /// <param name="beatID">The unique identifier of the beat to check; must not be null or empty</param>
        /// <returns>True if the beat has been visited, false otherwise</returns>
        public bool WasBeatVisited(string beatID)
        {
            // Exit case - the beat ID is invalid
            return !string.IsNullOrEmpty(beatID) && GetValue<bool>(string.Format(BeatVisitedFormat, beatID));
        }

        /// <summary>
        /// Retrieves the fragment associated with the specified beat identifier within the scene context;
        /// if no fragment is associated with the given beat ID, the method returns null
        /// </summary>
        /// <param name="beatID">The unique identifier for the beat whose fragment needs to be retrieved; this must not be null or empty</param>
        /// <returns>The fragment associated with the specified beat ID, or null if no fragment is found</returns>
        public string GetFragmentAtBeat(string beatID)
        {
            // Exit case - the beat ID is invalid
            return string.IsNullOrEmpty(beatID) ? null : GetValue<string>(string.Format(BeatFragmentFormat, beatID));
        }
    }
}
