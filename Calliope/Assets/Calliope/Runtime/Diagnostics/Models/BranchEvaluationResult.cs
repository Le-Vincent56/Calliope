using System.Collections.Generic;

namespace Calliope.Runtime.Diagnostics.Models
{
    /// <summary>
    /// Represents the evaluation result of a single branch, including all its conditions
    /// </summary>
    public class BranchEvaluationResult
    {
        /// <summary>
        /// The index of this branch in the beat's branch list (0-based)
        /// </summary>
        public int BranchIndex { get; }

        /// <summary>
        /// The beat ID this branch would transition to
        /// </summary>
        public string NextBeatID { get; }

        /// <summary>
        /// Whether this branch was the one selected for transition
        /// </summary>
        public bool WasSelected { get; }

        /// <summary>
        /// Whether all conditions in this branch passed
        /// </summary>
        public bool AllConditionsPassed { get; }

        /// <summary>
        /// Whether this is an unconditional branch (no conditions defined)
        /// </summary>
        public bool IsUnconditional { get; }

        /// <summary>
        /// The evaluation results for each individual condition in this branch
        /// </summary>
        public IReadOnlyList<ConditionEvaluationResult> ConditionResults { get; }

        public BranchEvaluationResult(
            int branchIndex,
            string nextBeatID,
            bool wasSelected,
            bool allConditionsPassed,
            bool isUnconditional,
            IReadOnlyList<ConditionEvaluationResult> conditionResults
        )
        {
            BranchIndex = branchIndex;
            NextBeatID = nextBeatID;
            WasSelected = wasSelected;
            AllConditionsPassed = allConditionsPassed;
            IsUnconditional = isUnconditional;
            ConditionResults = conditionResults ?? new List<ConditionEvaluationResult>();
        }
    }
}