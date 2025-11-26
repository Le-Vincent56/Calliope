namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Context for content selection, including recency tracking;
    /// prevents repetition by penalizing recently used content
    /// </summary>
    public interface ISelectionContext
    {
        /// <summary>
        /// Random number generator for weighted selection
        /// </summary>
        System.Random Random { get; }

        /// <summary>
        /// Check if content was recently used (within the recency window);
        /// Example: IsRecent("fragment_3") returns true if used in the last 5 selections
        /// </summary>
        /// <param name="contentID">The unique identifier of the content to be checked for recency</param>
        /// <returns>True if the content ID is recent; otherwise, false</returns>
        bool IsRecent(string contentID);

        /// <summary>
        /// Mark content as used, adding it to recency tracking; called after selection
        /// to update history
        /// </summary>
        /// <param name="contentID">The unique identifier of the content to be marked as used</param>
        void MarkUsed(string contentID);

        /// <summary>
        /// Retrieves the number of times the specified content has been used
        /// </summary>
        /// <param name="contentID">The unique identifier of the content whose usage count is to be retrieved</param>
        /// <returns>The number of times the specified content has been used</returns>
        int GetUseCount(string contentID);
        
        /// <summary>
        /// A multiplier applied to recent content (e.g., 0.5 = half-score);
        /// a lower multiplier means a stronger penalty against repetition
        /// </summary>
        float RecencyPenalty { get; set; }

        /// <summary>
        /// Resets the selection context to its initial state by clearing all tracked usage data,
        /// including usage counts and the recency queue, effectively removing all recorded history
        /// </summary>
        void Reset();
    }
}