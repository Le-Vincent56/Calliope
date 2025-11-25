using System.Text;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Builder for constructing scoring explanations step-by-step;
    /// tracks score modifications and generates human-readable text
    /// </summary>
    public class ScoringExplanationBuilder
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private float _currentScore = 1.0f;

        /// <summary>
        /// Sets the base score for the scoring system, clearing any previous modifications or explanations
        /// </summary>
        /// <param name="score">The initial score to set as the starting point for further modifications</param>
        public void SetBaseScore(float score)
        {
            _currentScore = score;
            _stringBuilder.AppendLine("Base score: ");
            _stringBuilder.Append(score.ToString("F2"));
        }

        /// <summary>
        /// Applies a trait-based affinity modifier to the current score if the specified trait is present
        /// </summary>
        /// <param name="traitID">The identifier of the trait being evaluated</param>
        /// <param name="weight">The weight value to be added to the current score if the trait is present</param>
        /// <param name="hasTrait">Indicates whether the specified trait is present</param>
        public void ApplyTraitAffinity(string traitID, float weight, bool hasTrait)
        {
            // Exit case - the trait doesn't apply
            if (!hasTrait) return;
            
            // Calculate the score
            float oldScore = _currentScore;
            _currentScore += weight;

            // Build the line
            _stringBuilder.AppendLine("\tTrait ");
            _stringBuilder.Append(traitID);
            _stringBuilder.Append(" (weight ");
            _stringBuilder.Append(weight.ToString("F2"));
            _stringBuilder.AppendLine("): ");
            _stringBuilder.Append(oldScore.ToString("F2"));
            _stringBuilder.Append(" + ");
            _stringBuilder.Append(weight.ToString("F2"));
            _stringBuilder.Append(" = ");
            _stringBuilder.Append(_currentScore.ToString("F2"));
        }

        /// <summary>
        /// Applies a relationship-based modifier to the current score if the specified condition is met
        /// </summary>
        /// <param name="threshold">The minimum value needed for the modifier to apply</param>
        /// <param name="multiplier">The multiplier to apply to the current score</param>
        /// <param name="actualValue">The actual value being compared against the threshold</param>
        /// <param name="met">Indicates whether the condition for applying the modifier is satisfied</param>
        public void ApplyRelationshipModifier(float threshold, float multiplier, float actualValue, bool met)
        {
            // Exit case - the relationship doesn't apply
            if (!met) return;

            float oldScore = _currentScore;
            _currentScore *= multiplier;

            // Build the line
            _stringBuilder.AppendLine("\tRelationship ");
            _stringBuilder.Append(actualValue.ToString("F2"));
            _stringBuilder.Append(" >= ");
            _stringBuilder.Append(threshold.ToString("F2"));
            _stringBuilder.AppendLine(" (x");
            _stringBuilder.Append(multiplier.ToString("F2"));
            _stringBuilder.AppendLine("): ");
            _stringBuilder.Append(oldScore.ToString("F2"));
            _stringBuilder.Append(" * ");
            _stringBuilder.Append(multiplier.ToString("F2"));
            _stringBuilder.Append(" = ");
            _stringBuilder.Append(_currentScore.ToString("F2"));
        }

        /// <summary>
        /// Marks the scoring process as invalid and appends the specified reason
        /// for invalidation to the explanation
        /// </summary>
        /// <param name="reason">The detailed reason why the scoring process is deemed invalid</param>
        public void MarkInvalid(string reason)
        {
            _currentScore = -1f;
            _stringBuilder.AppendLine("Invalid: ");
            _stringBuilder.Append(reason);
        }

        /// <summary>
        /// Constructs an instance of <see cref="IScoringResult"/> based on the current state
        /// of the builder, including the accumulated score and detailed explanation
        /// </summary>
        /// <returns>
        /// An <see cref="IScoringResult"/> instance that contains the calculated score
        /// and the explanation of scoring steps; the validity of the result depends on whether
        /// the score is greater than or equal to zero
        /// </returns>
        public IScoringResult Build() => ScoringResult.Create(_currentScore, _stringBuilder.ToString());
    }
}