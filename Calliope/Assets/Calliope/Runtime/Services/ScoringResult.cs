using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// The result of scoring a fragment;
    /// contains score and a detailed explanation for debugging
    /// </summary>
    public class ScoringResult : IScoringResult
    {
        public float Score { get; }
        public bool IsValid => Score >= 0;
        public string Explanation { get; }

        private ScoringResult(float score, string explanation)
        {
            Score = score;
            Explanation = explanation;
        }

        /// <summary>
        /// Creates a scoring result with the specified score and explanation
        /// </summary>
        /// <param name="score">The score value associated with the result</param>
        /// <param name="explanation">The explanation detailing the rationale behind the score</param>
        /// <returns>A <see cref="ScoringResult"/> instance with the specified score and explanation</returns>
        public static ScoringResult Create(float score, string explanation) => new ScoringResult(score, explanation);

        /// <summary>
        /// Creates an invalid scoring result with the specified reason
        /// </summary>
        /// <param name="reason">The reason why the scoring result is invalid</param>
        /// <returns>A <see cref="ScoringResult"/> instance with a score of -1 and the specified reason in the explanation</returns>
        public static ScoringResult Invalid(string reason)
        {
            StringBuilder reasonBuilder = new StringBuilder();
            reasonBuilder.Append("Invalid: ");
            reasonBuilder.Append(reason);

            return new ScoringResult(-1f, reasonBuilder.ToString());
        }
    }
}