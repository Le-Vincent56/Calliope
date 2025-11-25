using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents an individual narrative unit or "beat" within a scene's flow;
    /// encapsulates information about the speaker and target roles, potential
    /// variations, branching logic, and whether the beat concludes the sequence.
    /// </summary>
    public class SceneBeat : ISceneBeat
    {
        public string BeatID { get; set; }
        public string SpeakerRoleID { get; set; }
        public string TargetRoleID { get; set; }
        public string VariationSetID { get; set; }
        public IReadOnlyList<IBeatBranch> Branches { get; set; }
        public string DefaultNextBeatID { get; set; }
        public bool IsEndBeat { get; set; }
        
        public SceneBeat()
        {
            Branches = new List<IBeatBranch>();
        }
    }
}