using System;
using System.Text;
using Calliope.Editor.DialogueTimeline.Models;
using UnityEngine.UIElements;

namespace Calliope.Editor.DialogueTimeline.Views
{
    /// <summary>
    /// Visual element representing a single beat/entry in the timeline
    /// </summary>
    public class TimelineNodeView : VisualElement
    {
        private readonly Label _speakerLabel;
        private readonly Label _textLabel;
        
        public TimelineEntry Entry { get; }
        
        public event Action<TimelineNodeView> OnSelected;

        public TimelineNodeView(TimelineEntry entry)
        {
            Entry = entry;
            
            AddToClassList("timeline-node");
            
            // Start/end indicators
            if (entry.Index == 0)
                AddToClassList("timeline-node-start");

            if (entry.BeatTransition?.WasEndBeat == true)
                AddToClassList("timeline-node-end");

            // Header
            VisualElement header = new VisualElement();
            header.AddToClassList("timeline-node-header");

            Label titleLabel = new Label(entry.BeatID ?? "Unknown Beat");
            titleLabel.AddToClassList("timeline-node-title");
            header.Add(titleLabel);

            Label timestampLabel = new Label(entry.Timestamp.ToString("HH:mm:ss.fff"));
            timestampLabel.AddToClassList("timeline-node-timestamp");
            header.Add(timestampLabel);

            Add(header);

            // Body
            VisualElement body = new VisualElement();
            body.AddToClassList("timeline-node-body");

            _speakerLabel = new Label(FormatSpeaker(entry));
            _speakerLabel.AddToClassList("timeline-node-speaker");
            body.Add(_speakerLabel);

            _textLabel = new Label(TruncateText(entry.DialogueText, 120));
            _textLabel.AddToClassList("timeline-node-text");
            body.Add(_textLabel);

            StringBuilder indexBuilder = new StringBuilder();
            indexBuilder.Append("#");
            indexBuilder.Append(entry.Index + 1);
            
            Label indexLabel = new Label(indexBuilder.ToString());
            indexLabel.AddToClassList("timeline-node-index");
            body.Add(indexLabel);

            Add(body);

            // Click handler
            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>
        /// Handles the click event on the timeline node view, triggering the OnSelected action
        /// and stopping further propagation of the event
        /// </summary>
        /// <param name="evt">The click event that occurred on this visual element</param>
        private void OnClick(ClickEvent evt)
        {
            OnSelected?.Invoke(this);
            evt.StopPropagation();
        }

        /// <summary>
        /// Updates the visual state of the node to indicate whether it is selected
        /// </summary>
        /// <param name="selected">
        /// A boolean value indicating whether the node should be marked as selecte;
        /// if true, the node will appear in a selected state; if false, the selection state will be removed
        /// </param>
        public void SetSelected(bool selected)
        {
            // Exit case - selecting
            if (selected)
            {
                AddToClassList("timeline-node-selected");
                return;
            }
            
            RemoveFromClassList("timeline-node-selected");
        }

        /// <summary>
        /// Refreshes the displayed content of the node view to match the current state of its associated timeline entry;
        /// updates the speaker label and dialogue text shown on the node
        /// </summary>
        public void UpdateContent()
        {
            _speakerLabel.text = FormatSpeaker(Entry);
            _textLabel.text = TruncateText(Entry.DialogueText, 120);
        }

        /// <summary>
        /// Formats the speaker information for a timeline entry, combining the speaker's name and role ID
        /// into a single string, based on the available data
        /// </summary>
        /// <param name="entry">The timeline entry containing speaker information to format</param>
        /// <returns>
        /// A formatted string representing the speaker, including their role if available,
        /// or a fallback string if no speaker information is provided
        /// </returns>
        private string FormatSpeaker(TimelineEntry entry)
        {
            // Exit case - no speaker specified
            if (string.IsNullOrEmpty(entry.SpeakerName) && string.IsNullOrEmpty(entry.SpeakerRoleID))
                return "(No speaker)";

            // Exit case - only speaker name specified
            if (string.IsNullOrEmpty(entry.SpeakerName))
                return entry.SpeakerRoleID;

            // Exit case - no speaker role specified
            if (string.IsNullOrEmpty(entry.SpeakerRoleID)) return entry.SpeakerName;
            
            StringBuilder nameBuilder = new StringBuilder();
            nameBuilder.Append(entry.SpeakerName);
            nameBuilder.Append(" (");
            nameBuilder.Append(entry.SpeakerRoleID);
            nameBuilder.Append(")");
                
            return nameBuilder.ToString();
        }

        /// <summary>
        /// Truncates the given text to the specified maximum length; if truncation occurs,
        /// the resulting string is suffixed with ellipses ("..."). If the input text is null or empty,
        /// a default placeholder message is returned
        /// </summary>
        /// <param name="text">The input text to be truncated</param>
        /// <param name="maxLength">The maximum allowable length for the truncated string, including the ellipses</param>
        /// <returns>
        /// A string that is either the original text (if its length is within the limit) or
        /// a truncated version of it with ellipses
        /// </returns>
        private string TruncateText(string text, int maxLength)
        {
            // Exit case - no text
            if (string.IsNullOrEmpty(text))
                return "(No dialogue yet)";

            // Exit case - the text does not need to be truncated
            if (text.Length <= maxLength) return text;
            
            // Truncate the text
            StringBuilder truncatedBuilder = new StringBuilder();
            truncatedBuilder.Append(text[..maxLength]);
            truncatedBuilder.Append("...");

            return truncatedBuilder.ToString();
        }
    }
}
