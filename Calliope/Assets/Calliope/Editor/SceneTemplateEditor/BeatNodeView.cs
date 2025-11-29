using System.Text;
using Calliope.Unity.ScriptableObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Represents a visual element for a beat node within an editor environment;
    /// it is used to display and manipulate a `SceneBeatSO` in a graphical editor view
    /// </summary>
    public class BeatNodeView : VisualElement
    {
        private SceneTemplateEditorWindow _window;
        private readonly SceneBeatSO _beat;
        private Label _beatIDLabel;
        private Label _speakerLabel;
        private Label _variationSetLabel;
        private VisualElement _header;
        private VisualElement _body;

        private VisualElement _outputPort;
        private VisualElement _inputPort;
        
        public SceneBeatSO Beat => _beat;
        public Vector2 Position { get; set; }
        public VisualElement OutputPort => _outputPort;
        public VisualElement InputPort => _inputPort;
        
        public BeatNodeView(SceneBeatSO beat, Vector2 position, SceneTemplateEditorWindow window)
        {
            _beat = beat;
            Position = position;
            _window = window;
            
            // Add USS class for styling
            AddToClassList("beat-node");
            
            // Set position
            style.position = UnityEngine.UIElements.Position.Absolute;
            style.left = position.x;
            style.top = position.y;
            style.width = 200;

            // Build the node UI
            BuildNode();
            
            // Add drag manipulator
            this.AddManipulator(new NodeDragManipulator(this, window));
        }

        /// <summary>
        /// Constructs the visual structure of the beat node
        /// </summary>
        private void BuildNode()
        {
            // Header section (contains Beat ID)
            _header = new VisualElement();
            _header.AddToClassList("beat-node-header");
            
            // Create the input port (left side)
            _inputPort = new VisualElement();
            _inputPort.AddToClassList("node-port");
            _inputPort.AddToClassList("input-port");
            _header.Add(_inputPort);

            _beatIDLabel = new Label(_beat ? _beat.BeatID : "NULL");
            _beatIDLabel.AddToClassList("beat-node-title");
            _header.Add(_beatIDLabel);
            
            // Create the output port (right side)
            _outputPort = new VisualElement();
            _outputPort.AddToClassList("node-port");
            _outputPort.AddToClassList("output-port");
            _header.Add(_outputPort);
            
            Add(_header);
            
            // Body section (contains details)
            _body = new VisualElement();
            _body.AddToClassList("beat-node-body");
            
            // Exit case - the beat is null
            if (!_beat)
            {
                Label errorLabel = new Label("Beat is null");
                errorLabel.style.color = Color.red;
                _body.Add(errorLabel);
                return;
            }
            
            StringBuilder labelBuilder = new StringBuilder();
            
            // Speaker info
            labelBuilder.Append("Speaker: ");
            labelBuilder.Append(_beat.SpeakerRoleID ?? "None");
            _speakerLabel = new Label(labelBuilder.ToString());
            _speakerLabel.AddToClassList("beat-node-field");
            _body.Add(_speakerLabel);
            
            // Target info (if exists)
            if (!string.IsNullOrWhiteSpace(_beat.TargetRoleID))
            {
                // Build the label
                labelBuilder.Clear();
                labelBuilder.Append("Target: ");
                labelBuilder.Append(_beat.TargetRoleID ?? "None");
                Label targetLabel = new Label(labelBuilder.ToString());
                targetLabel.AddToClassList("beat-node-field");
                _body.Add(targetLabel);
            }
            
            // Variation set info
            labelBuilder.Clear();
            labelBuilder.Append("Variation set: ");
            labelBuilder.Append(_beat.VariationSetID ?? "None");
            _variationSetLabel = new Label(labelBuilder.ToString());
            _variationSetLabel.AddToClassList("beat-node-field");
            _body.Add(_variationSetLabel);
            
            // Branches info
            if (_beat.Branches != null && _beat.Branches.Count > 0)
            {
                labelBuilder.Clear();
                labelBuilder.Append("Brnaches: ");
                labelBuilder.Append(_beat.Branches.Count);
                Label branchesLabel = new Label(labelBuilder.ToString());
                branchesLabel.AddToClassList("beat-node-field");
                _body.Add(branchesLabel);
            }
            
            Add(_body);
        }

        /// <summary>
        /// Updates the visual styling of the beat node to indicate its selection state
        /// </summary>
        /// <param name="selected">A boolean indicating whether the beat node should be visually marked as selected (true) or deselected (false)</param>
        public void SetSelected(bool selected)
        {
            if (selected) AddToClassList("beat-node-selected");
            else RemoveFromClassList("beat-node-selected");
        }

        /// <summary>
        /// Updates the position of the beat node within the visual editor
        /// </summary>
        /// <param name="newPosition">The new position to set for the beat node, represented as a 2D vector (x, y)</param>
        public void SetPosition(Vector2 newPosition)
        {
            Position = newPosition;
            style.left = newPosition.x;
            style.top = newPosition.y;
        }

        /// <summary>
        /// Updates the visual styling of the beat node to indicate whether it is the starting beat
        /// </summary>
        /// <param name="isStarting">A boolean indicating whether the beat node should be marked as the starting beat (true) or not (false)</param>
        public void SetStartingBeat(bool isStarting)
        {
            if (isStarting) AddToClassList("starting-beat");
            else RemoveFromClassList("starting-beat");
        }
    }
}
