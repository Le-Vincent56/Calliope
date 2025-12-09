using System;
using Calliope.Runtime.Diagnostics.Events;

namespace Calliope.Editor.DialogueTimeline.Models
{
    /// <summary>
    /// Represents a single entry in the dialogue timeline
    /// </summary>
    public class TimelineEntry
    {
        /// <summary>
        /// Index in the timeline (0-based)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// When this entry was recorded
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// The beat ID for this entry
        /// </summary>
        public string BeatID { get; }

        /// <summary>
        /// The speaker's display name
        /// </summary>
        public string SpeakerName { get; set; }

        /// <summary>
        /// The speaker's role ID
        /// </summary>
        public string SpeakerRoleID { get; set; }

        /// <summary>
        /// The assembled dialogue text
        /// </summary>
        public string DialogueText { get; set; }

        /// <summary>
        /// The beat transition event data (for branch evaluation details)
        /// </summary>
        public DiagnosticBeatTransitionEvent BeatTransition { get; set; }

        /// <summary>
        /// The fragment selection event data (for scoring details)
        /// </summary>
        public DiagnosticFragmentSelectionEvent FragmentSelection { get; set; }

        public TimelineEntry(string beatID)
        {
            Timestamp = DateTime.Now;
            BeatID = beatID;
        }
    }
}
