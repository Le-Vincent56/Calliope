using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A conditional branch from one beat to another;
    /// Example: "If Instigator has ["leader"] trait, go to beat_3a"
    /// </summary>
    public interface IBeatBranch
    {
        /// <summary>
        /// The beat to transition to if all conditions are met
        /// </summary>
        string NextBeatID { get; }
        
        /// <summary>
        /// All conditions that must be true for this branch to be taken;
        /// an empty list means it is an unconditional branch
        /// </summary>
        IReadOnlyList<IBranchCondition> Conditions { get; }
    }
}