using UnityEngine.UIElements;

namespace Calliope.Editor.DialogueTimeline.Models
{
    /// <summary>
    /// Represents the UI elements associated with an individual timeline item
    /// </summary>
    public class TimelineItemElements
    {
        public Label Index;
        public Label Beat;
        public Label Speaker;
        public Label Text;
        public Label Timestamp;
        public VisualElement Root;
    }
}