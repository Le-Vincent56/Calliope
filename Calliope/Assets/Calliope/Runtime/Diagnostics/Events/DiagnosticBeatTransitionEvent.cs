using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Infrastructure.Events;
using Calliope.Runtime.Diagnostics.Models;

namespace Calliope.Runtime.Diagnostics.Events
{
    /// <summary>
    /// Published when advancing between beats, containing full branch evaluation details
    /// </summary>
    public class DiagnosticBeatTransitionEvent : CalliopeEventBase
    {
        /// <summary>
        /// The ID of the scene being played
        /// </summary>
        public string SceneID { get; }

        /// <summary>
        /// The beat ID we transitioned from
        /// </summary>
        public string FromBeatID { get; }

        /// <summary>
        /// The beat ID we transitioned to
        /// </summary>
        public string ToBeatID { get; }

        /// <summary>
        /// Whether the default fallback was used (no branch conditions matched)
        /// </summary>
        public bool UsedDefaultFallback { get; }

        /// <summary>
        /// Whether this was an end beat (scene completed)
        /// </summary>
        public bool WasEndBeat { get; }

        /// <summary>
        /// Evaluation results for all branches that were considered
        /// </summary>
        public IReadOnlyList<BranchEvaluationResult> BranchEvaluations { get; }

        /// <summary>
        /// The current cast for context display
        /// </summary>
        public IReadOnlyDictionary<string, ICharacter> Cast { get; }

        public DiagnosticBeatTransitionEvent(
            string sceneID,
            string fromBeatID,
            string toBeatID,
            bool usedDefaultFallback,
            bool wasEndBeat,
            IReadOnlyList<BranchEvaluationResult> branchEvaluations,
            IReadOnlyDictionary<string, ICharacter> cast)
        {
            SceneID = sceneID;
            FromBeatID = fromBeatID;
            ToBeatID = toBeatID;
            UsedDefaultFallback = usedDefaultFallback;
            WasEndBeat = wasEndBeat;
            BranchEvaluations = branchEvaluations ?? new List<BranchEvaluationResult>();
            Cast = cast;
        }
    }
}