using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a conditional branch within a narrative system that determines the
    /// next beat based on specific conditions
    /// </summary>
    public class BeatBranch : IBeatBranch
    {
        public string NextBeatID { get; set; }
        public IReadOnlyList<IBranchCondition> Conditions { get; set; }

        public BeatBranch()
        {
            Conditions = new List<IBranchCondition>();
        }
    }
}