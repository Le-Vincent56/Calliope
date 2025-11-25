using System;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Caching
{
    /// <summary>
    /// Specialized cache for fragment scoring results;
    /// Key = (FragmentID, SpeakerID, TargetID)
    /// </summary>
    public class ScoringCache
    {
        /// <summary>
        /// Represents the unique key for identifying scoring results in a cache;
        /// The key is composed of three components: FragmentID, SpeakerID, and TargetID
        /// </summary>
        private struct ScoringCacheKey
        {
            public readonly string FragmentID;
            public readonly string SpeakerID;
            public readonly string TargetID;

            public ScoringCacheKey(string fragmentID, string speakerID, string targetID)
            {
                FragmentID = fragmentID;
                SpeakerID = speakerID;
                TargetID = targetID ?? "";
            }

            /// <summary>
            /// Determines whether the specified ScoringCacheKey is equal to the current instance;
            /// The equality check is based on the values of FragmentID, SpeakerID, and TargetID
            /// </summary>
            /// <param name="other">The ScoringCacheKey to compare with the current instance</param>
            /// <returns>
            /// True if the specified ScoringCacheKey has the same FragmentID, SpeakerID,
            /// and TargetID as the current instance; otherwise, false
            /// </returns>
            public bool Equals(ScoringCacheKey other) => FragmentID == other.FragmentID
                                                         && SpeakerID == other.SpeakerID 
                                                         && TargetID == other.TargetID;

            /// <summary>
            /// Determines whether the specified object is equal to the current ScoringCacheKey instance;
            /// The equality check is based on the values of FragmentID, SpeakerID, and TargetID
            /// </summary>
            /// <param name="obj">The object to compare with the current ScoringCacheKey instance </param>
            /// <returns>
            /// True if the specified object is a ScoringCacheKey and has the same FragmentID, SpeakerID,
            /// and TargetID as the current instance; otherwise, false
            /// </returns>
            public override bool Equals(object obj) => obj is ScoringCacheKey other && Equals(other);

            /// <summary>
            /// Computes a hash code for the current ScoringCacheKey instance;
            /// this hash code is based on the values of FragmentID, SpeakerID, and TargetID
            /// and is used to uniquely identify this instance in hash-based collections
            /// </summary>
            /// <returns>
            /// A 32-bit signed integer hash code representing the current ScoringCacheKey
            /// </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (FragmentID?.GetHashCode() ?? 0);
                    hash = hash * 31 + (SpeakerID?.GetHashCode() ?? 0);
                    hash = hash * 31 + (TargetID?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }
        
        private readonly LRUCache<ScoringCacheKey, IScoringResult> _cache;

        public ScoringCache(int capacity = 1000)
        {
            _cache = new LRUCache<ScoringCacheKey, IScoringResult>(capacity);
        }

        /// <summary>
        /// Attempts to retrieve a scoring result from the cache based on the specified key components;
        /// Returns true if the scoring result exists in the cache; otherwise, returns false
        /// </summary>
        /// <param name="fragmentID">The unique identifier of the dialogue fragment</param>
        /// <param name="speakerID">The unique identifier of the speaker associated with the fragment</param>
        /// <param name="targetID">The unique identifier of the target associated with the fragment</param>
        /// <param name="result">When this method returns, contains the retrieved scoring result if found; otherwise, contains null</param>
        /// <returns>
        /// True if the specified scoring result exists in the cache; otherwise, false
        /// </returns>
        public bool TryGet(string fragmentID, string speakerID, string targetID, out IScoringResult result)
        {
            ScoringCacheKey key = new ScoringCacheKey(fragmentID, speakerID, targetID);
            return _cache.TryGetValue(key, out result);
        }

        /// <summary>
        /// Adds a scoring result to the cache, associating it with the specified key
        /// constructed from the given fragment ID, speaker ID, and target ID;
        /// if the key already exists in the cache, the existing value will be replaced
        /// </summary>
        /// <param name="fragmentID">The identifier of the fragment to associate with the scoring result</param>
        /// <param name="speakerID">The identifier of the speaker to associate with the scoring result</param>
        /// <param name="targetID">The identifier of the target to associate with the scoring result</param>
        /// <param name="result">The scoring result to be added to the cache</param>
        public void Add(string fragmentID, string speakerID, string targetID, IScoringResult result)
        {
            ScoringCacheKey key = new ScoringCacheKey(fragmentID, speakerID, targetID);
            _cache.Add(key, result);
        }

        /// <summary>
        /// Retrieves the scoring result for the specified combination of FragmentID, SpeakerID, and TargetID
        /// from the cache, or computes it using the provided function if it is not already cached
        /// </summary>
        /// <param name="fragmentID">The identifier for the dialogue fragment</param>
        /// <param name="speakerID">The identifier for the speaker associated with the scoring result</param>
        /// <param name="targetID">The identifier for the target associated with the scoring result</param>
        /// <param name="computeFunc">A function to compute the scoring result if it is not found in the cache</param>
        /// <returns>
        /// The scoring result either retrieved from the cache or computed using the provided function
        /// </returns>
        public IScoringResult GetOrCompute(
            string fragmentID,
            string speakerID,
            string targetID,
            Func<IScoringResult> computeFunc
        )
        {
            ScoringCacheKey key = new ScoringCacheKey(fragmentID, speakerID, targetID);
            return _cache.GetOrAdd(key, _ => computeFunc());
        }

        /// <summary>
        /// Clears all entries from the scoring cache, removing all cached scoring results;
        /// this operation resets the cache to its initial, empty state
        /// </summary>
        public void Clear() => _cache.Clear();
    }
}