using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A single moment/beat in a scene's narrative flow;
    /// Example: "Instigator proposes a dangerous route"
    /// </summary>
    public interface ISceneBeat
    {
        /// <summary>
        /// Unique identifier ("beat_1", "beat_4a"
        /// </summary>
        string BeatID { get; }
        
        /// <summary>
        /// Which role speaks this beat;
        /// Example: "instigator"
        /// </summary>
        string SpeakerRoleID { get; }
        
        /// <summary>
        /// Which role is being spoken to (can be null for addressing all);
        /// Example: "objector" or null
        /// </summary>
        string TargetRoleID { get; }
        
        /// <summary>
        /// ID of the variation set to select dialogue from;
        /// Example: "instigator_proposal"
        /// </summary>
        string VariationSetID { get; }
        
        /// <summary>
        /// Possible conditional branches from this beat;
        /// evaluated in order; the first matching branch is taken
        /// </summary>
        IReadOnlyList<IBeatBranch> Branches { get; }
        
        /// <summary>
        /// Default next beat if not branches match; null if this is an end beat
        /// </summary>
        string DefaultNextBeatID { get; }
        
        /// <summary>
        /// Determines whether this beat ends the scene
        /// </summary>
        bool IsEndBeat { get; }
    }
}