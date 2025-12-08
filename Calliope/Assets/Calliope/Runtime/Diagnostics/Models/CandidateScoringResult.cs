namespace Calliope.Runtime.Diagnostics.Models
{
    /// <summary>
    /// Represents the scoring result for a single fragment candidate
    /// </summary>
    public class CandidateScoringResult
    {
        /// <summary>
        /// The unique identifier of the fragment
        /// </summary>
        public string FragmentID { get; }

        /// <summary>
        /// The raw text template of the fragment (before variable substitution)
        /// </summary>
        public string FragmentText { get; }

        /// <summary>
        /// The calculated score for this fragment
        /// </summary>
        public float Score { get; }

        /// <summary>
        /// Whether this fragment is valid for the current speaker
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Full scoring breakdown explaining how the score was calculated
        /// </summary>
        public string ScoringExplanation { get; }

        /// <summary>
        /// Whether this fragment was the one ultimately selected
        /// </summary>
        public bool WasSelected { get; }

        public CandidateScoringResult(
            string fragmentID,
            string fragmentText,
            float score,
            bool isValid,
            string scoringExplanation,
            bool wasSelected
        )
        {
            FragmentID = fragmentID;
            FragmentText = fragmentText;
            Score = score;
            IsValid = isValid;
            ScoringExplanation = scoringExplanation ?? string.Empty;
            WasSelected = wasSelected;
        }
    }
}