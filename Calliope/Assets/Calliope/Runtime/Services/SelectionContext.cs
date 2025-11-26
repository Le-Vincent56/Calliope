using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Tracks content usage for recency-based selection;
    /// maintains a sliding window of recently used content
    /// </summary>
    public class SelectionContext : ISelectionContext
    {
        private readonly Dictionary<string, int> _useCounts = new Dictionary<string, int>();
        private readonly Queue<string> _recentQueue = new Queue<string>();
        private readonly int _recencyWindow;
        
        public System.Random Random { get; }
        public float RecencyPenalty { get; set; }

        public SelectionContext(int? seed = null, int recencyWindow = 5, float recencyPenalty = 0.5f)
        {
            Random = seed.HasValue 
                ? new System.Random(seed.Value) 
                : new System.Random();
            
            _recencyWindow = recencyWindow;
            RecencyPenalty = recencyPenalty;
        }

        /// <summary>
        /// Determines whether the specified content ID is present in the recent usage queue,
        /// indicating that it has been recently used
        /// </summary>
        /// <param name="contentID">The unique identifier of the content to check for recency</param>
        /// <returns>True if the content ID is found in the recent usage queue; otherwise, false</returns>
        public bool IsRecent(string contentID)
        {
            // Check if content is in the recent queue
            foreach (string id in _recentQueue)
            {
                // Exit case - the content is recent
                if(id == contentID) return true;
            }

            return false;
        }

        /// <summary>
        /// Marks the specified content ID as used by updating its usage count and adding it to the recent usage queue;
        /// this operation ensures that the recency tracking mechanism adheres to the configured window size,
        /// discarding the oldest entries when the queue exceeds the defined limit
        /// </summary>
        /// <param name="contentID">The unique identifier of the content to mark as recently used</param>
        public void MarkUsed(string contentID)
        {
            // Track use count
            _useCounts.TryAdd(contentID, 0);
            
            // Increment the usage
            _useCounts[contentID]++;
            
            // Add to recent queue
            _recentQueue.Enqueue(contentID);
            
            // Maintain window size
            while (_recentQueue.Count > _recencyWindow)
            {
                _recentQueue.Dequeue();
            }
        }

        /// <summary>
        /// Retrieves the usage count for a specified content ID, representing the number of times it has been marked as used
        /// </summary>
        /// <param name="contentID">The unique identifier of the content for which the usage count is requested</param>
        /// <returns>The number of times the specified content ID has been used. Returns 0 if the content ID is not found</returns>
        public int GetUseCount(string contentID) => _useCounts.GetValueOrDefault(contentID, 0);

        /// <summary>
        /// Resets the selection context to its initial state by clearing all tracked usage data,
        /// including usage counts and the recency queue, effectively removing all recorded history
        /// </summary>
        public void Reset()
        {
            _useCounts.Clear();
            _recentQueue.Clear();
        }
    }
}