using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Generic key-value store for scene state; external conditions
    /// can set custom keys like "tension_level", "votes.seal_breach", etc.
    /// </summary>
    public interface ISceneContext
    {
        /// <summary>
        /// Stores the specified value in the context store associated with the given key;
        /// if the key already exists, its value will be updated
        /// </summary>
        /// <param name="key">The key under which the value will be stored or updated in the context store</param>
        /// <param name="value">The value to be stored or updated in the context store</param>
        void SetValue(string key, object value);

        /// <summary>
        /// Retrieves the value associated with the specified key from the context store or returns a default value if the key does not exist
        /// </summary>
        /// <typeparam name="T">The type of the value to be retrieved</typeparam>
        /// <param name="key">The key associated with the value to be retrieved</param>
        /// <param name="defaultValue">The value to return if the key does not exist in the store</param>
        /// <returns>The value associated with the specified key, or the provided default value if the key does not exist</returns>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// Checks if the specified key exists in the context store
        /// </summary>
        /// <param name="key">The key to check for existence in the store</param>
        /// <returns>True if the key exists, otherwise false</returns>
        bool HasKey(string key);

        /// <summary>
        /// Removes the specified key from the context store if it exists
        /// </summary>
        /// <param name="key">The key to be removed from the store</param>
        /// <returns>True if the key was removed successfully, otherwise false</returns>
        bool RemoveKey(string key);

        /// <summary>
        /// Retrieves all keys currently stored in the context
        /// </summary>
        /// <returns>Enumerable collection containing all keys in the store</returns>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// Retrieves all keys from the store that start with the specified prefix
        /// </summary>
        /// <param name="prefix">The prefix that the keys should start with</param>
        /// <returns>Enumerable collection containing keys that match the specified prefix</returns>
        IEnumerable<string> GetKeysWithPrefix(string prefix);

        /// <summary>
        /// Clears all values
        /// </summary>
        void Clear();

        /// <summary>
        /// Increments the value associated with the specified key by a given amount;
        /// if the key does not exist, it is initialized with the increment value
        /// </summary>
        /// <param name="key">The unique key whose associated value should be incremented</param>
        /// <param name="incrementBy">The amount by which to increment the value; defaults to 1 if not specified</param>
        void Increment(string key, float incrementBy = 1f);

        /// <summary>
        /// Records the visit of a specific beat within the scene context, associating it
        /// with a fragment ID and a speaker role ID
        /// </summary>
        /// <param name="beatID">The unique identifier for the beat being recorded</param>
        /// <param name="fragmentID">The unique identifier of the fragment associated with the beat</param>
        /// <param name="speakerRoleID">The unique identifier of the speaker role related to the beat</param>
        void RecordBeatVisit(string beatID, string fragmentID, string speakerRoleID);

        /// <summary>
        /// Determines whether a specific beat has been visited in the scene context
        /// </summary>
        /// <param name="beatID">The unique identifier for the beat to check</param>
        /// <returns>
        /// Returns true if the specified beat has been visited; otherwise, false
        /// </returns>
        bool WasBeatVisited(string beatID);

        /// <summary>
        /// Retrieves the fragment associated with a specific beat in the scene context
        /// </summary>
        /// <param name="beatID">The unique identifier for the beat whose fragment is to be retrieved</param>
        /// <returns>
        /// Returns the fragment ID associated with the specified beat if it exists,
        /// or null if no fragment is found
        /// </returns>
        string GetFragmentAtBeat(string beatID);
    }
}