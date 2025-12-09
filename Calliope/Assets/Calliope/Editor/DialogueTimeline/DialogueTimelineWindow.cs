using System.Collections.Generic;
using System.Text;
using Calliope.Editor.DialogueTimeline.Models;
using Calliope.Editor.DialogueTimeline.Views;
using Calliope.Runtime.Diagnostics;
using Calliope.Runtime.Diagnostics.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.DialogueTimeline
{
    /// <summary>
    /// Represents a custom Unity editor window for managing and visualizing
    /// dialogue timelines in the Calliope Dialogue system; this tool
    /// is designed to assist developers and designers in creating and
    /// editing dialogue sequences within the Unity Editor
    /// </summary>
    public class DialogueTimelineWindow : EditorWindow
    {
        private readonly List<TimelineEntry> _entries = new List<TimelineEntry>();
        private readonly List<TimelineNodeView> _nodeViews = new List<TimelineNodeView>();
        private TimelineEntry _selectedEntry;
        private TimelineNodeView _selectedNodeView;

        private bool _autoScroll = true;
        private bool _isConnected;
        private DiagnosticBeatTransitionEvent _pendingBeatTransition;

        private Label _statusLabel;
        private ScrollView _timelineScrollView;
        private VisualElement _timelineGraph;
        private ScrollView _detailScrollView;
        private Label _emptyLabel;

        private void OnEnable()
        {
            BuildUI();
            
            // Subscribe to Play Mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // If already in Play Mode, try to connect
            if (EditorApplication.isPlaying)
                EditorApplication.delayCall += TryConnect;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Disconnect();
        }

        /// <summary>
        /// Opens the Dialogue Timeline window within the Unity Editor;
        /// configures the window's title and ensures a minimum size requirement
        /// to accommodate all necessary user interface elements
        /// </summary>
        [MenuItem("Window/Calliope/Dialogue Timeline")]
        public static void ShowWindow()
        {
            DialogueTimelineWindow window = GetWindow<DialogueTimelineWindow>();
            window.titleContent = new GUIContent("Dialogue Timeline");
            window.minSize = new Vector2(900, 400);
        }

        /// <summary>
        /// Constructs and initializes the user interface elements for the Dialogue Timeline window;
        /// this includes setting up the toolbar with controls for clearing the timeline and toggling
        /// the auto-scroll option, as well as creating layout structures for displaying the timeline
        /// graph and detail view panel
        /// </summary>
        private void BuildUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.AddToClassList("timeline-root");

            // Load stylesheet
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Calliope/Editor/DialogueTimeline/DialogueTimelineWindow.uss"
            );
            if (styleSheet)
                root.styleSheets.Add(styleSheet);

            // Toolbar
            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("timeline-toolbar");

            Button clearButton = new Button(ClearTimeline) { text = "Clear" };
            clearButton.AddToClassList("timeline-toolbar-button");
            toolbar.Add(clearButton);

            Toggle autoScrollToggle = new Toggle("Auto-scroll") { value = _autoScroll };
            autoScrollToggle.RegisterValueChangedCallback(evt => _autoScroll = evt.newValue); 
            autoScrollToggle.AddToClassList("timeline-toolbar-toggle");
            toolbar.Add(autoScrollToggle);

            _statusLabel = new Label("Disconnected");
            _statusLabel.AddToClassList("timeline-toolbar-status");
            _statusLabel.AddToClassList("timeline-status-disconnected");
            toolbar.Add(_statusLabel);

            root.Add(toolbar);

            // Content area
            VisualElement content = new VisualElement();
            content.AddToClassList("timeline-content");

            // Timeline scroll view
            _timelineScrollView = new ScrollView(ScrollViewMode.Horizontal);
            _timelineScrollView.AddToClassList("timeline-scroll-view");

            _timelineGraph = new VisualElement();
            _timelineGraph.AddToClassList("timeline-graph");

            _emptyLabel = new Label("Enter Play Mode and run a scene to see the dialogue timeline"); 
            _emptyLabel.AddToClassList("timeline-empty-text");
            _timelineGraph.Add(_emptyLabel);

            _timelineScrollView.Add(_timelineGraph);
            content.Add(_timelineScrollView);

            // Detail panel
            VisualElement detailPanel = new VisualElement();
            detailPanel.AddToClassList("detail-panel");

            // Detail header
            VisualElement detailHeader = new VisualElement();
            detailHeader.AddToClassList("detail-panel-header");
            
            // Title
            Label detailTitle = new Label("Details");
            detailTitle.AddToClassList("detail-panel-title");
            detailHeader.Add(detailTitle);
            detailPanel.Add(detailHeader);

            // Detail scroll view
            _detailScrollView = new ScrollView();
            _detailScrollView.AddToClassList("detail-panel-content");

            Label placeholder = new Label("Select a node to view details"); 
            placeholder.AddToClassList("detail-placeholder");
            _detailScrollView.Add(placeholder);

            detailPanel.Add(_detailScrollView);
            content.Add(detailPanel);

            root.Add(content);
        }

        /// <summary>
        /// Handles changes in the Play Mode state and updates the Dialogue Timeline window accordingly;
        /// triggers connection attempts when entering Play Mode, and handles disconnection and cleanup
        /// when exiting Play Mode
        /// </summary>
        /// <param name="state">
        /// The current Play Mode state transition event, indicating whether Play Mode
        /// has been entered or exited
        /// </param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall += TryConnect;
                    break;
                
                case PlayModeStateChange.ExitingPlayMode:
                    Disconnect();
                    ClearTimeline();
                    break;
            }
        }

        /// <summary>
        /// Attempts to establish a connection between the Dialogue Timeline window and the Calliope instance;
        /// this method ensures the editor is in Play Mode before proceeding; if a Calliope instance is found,
        /// diagnostics are enabled, necessary events are subscribed to for timeline updates, and the connection
        /// status is updated; logs a warning if no Calliope instance exists in the scene
        /// </summary>
        private void TryConnect()
        {
            // Exit case - the Editor is not in Play Mode
            if (!EditorApplication.isPlaying) return;

            Unity.Components.Calliope calliope = Unity.Components.Calliope.Instance;
            
            // Exit case - no Calliope instance exists
            if (!calliope)
            {
                Debug.LogWarning("[DialogueTimeline] Calliope instance not found. Make sure a Calliope component exists in the scene.");
                return;
            }
            
            // Enable diagnostics
            DiagnosticsManager.Instance.Enable();
            
            // Subscribe to events
            calliope.EventBus.Subscribe<DiagnosticBeatTransitionEvent>(OnBeatTransition);
            calliope.EventBus.Subscribe<DiagnosticFragmentSelectionEvent>(OnFragmentSelection);

            _isConnected = true;
            UpdateStatus();
        }

        /// <summary>
        /// Disconnects the current connection between the editor window and the Calliope instance;
        /// this method unsubscribes from diagnostic events, disables diagnostics,
        /// and updates the connection status in the UI; it ensures no further interactions
        /// occur with the Calliope instance while disconnected
        /// </summary>
        private void Disconnect()
        {
            // Exit case - not connected
            if (!_isConnected) return;
            
            Unity.Components.Calliope calliope = Unity.Components.Calliope.Instance;
            
            if (calliope)
            {
                // Disable diagnostics
                DiagnosticsManager.Instance.Disable();
                calliope.EventBus.Unsubscribe<DiagnosticBeatTransitionEvent>(OnBeatTransition);
                calliope.EventBus.Unsubscribe<DiagnosticFragmentSelectionEvent>(OnFragmentSelection);
            }

            _isConnected = false;
            UpdateStatus();
        }

        /// <summary>
        /// Updates the status label in the UI to reflect the current connection state,
        /// styling it as "Connected" or "Disconnected" based on whether the system is
        /// connected to the Calliope instance
        /// </summary>
        private void UpdateStatus()
        {
            // Exit case - no status label exists
            if (_statusLabel == null)
                return;

            // Clear the status styling
            _statusLabel.RemoveFromClassList("timeline-status-connected");
            _statusLabel.RemoveFromClassList("timeline-status-disconnected");

            // Style the status based on connection
            if (_isConnected)
            {
                _statusLabel.text = "Connected";
                _statusLabel.AddToClassList("timeline-status-connected");
            }
            else
            {
                _statusLabel.text = "Disconnected";
                _statusLabel.AddToClassList("timeline-status-disconnected");
            }
        }

        /// <summary>
        /// Handles the diagnostic event triggered during a beat transition within the Calliope Dialogue system;
        /// stores the details of the transition event for further processing or visualization
        /// in the Dialogue Timeline window
        /// </summary>
        /// <param name="evt">
        /// The diagnostic event containing information about the beat transition,
        /// including the originating beat, the destination beat, branch evaluations, and associated cast details
        /// </param>
        private void OnBeatTransition(DiagnosticBeatTransitionEvent evt)
        {
            _pendingBeatTransition = evt;
        }

        /// <summary>
        /// Handles the selection of a dialogue fragment by updating the corresponding timeline entry with
        /// fragment data, speaker information, and assembled dialogue text; if no matching entry is found,
        /// a new one is created and added to the timeline; updates visual components such as the node view
        /// and detail panel if necessary
        /// </summary>
        /// <param name="evt">
        /// The event containing fragment selection details, including the speaker, assembled text,
        /// and related beat information
        /// </param>
        private void OnFragmentSelection(DiagnosticFragmentSelectionEvent evt)
        {
            string beatID = _pendingBeatTransition?.ToBeatID
                            ?? _pendingBeatTransition?.FromBeatID
                            ?? evt.BeatID
                            ?? "Unknown";

            // Create entry with both transition and fragment data
            TimelineEntry entry = new TimelineEntry(beatID)
            {
                Index = _entries.Count,
                BeatTransition = _pendingBeatTransition,
                FragmentSelection = evt,
                SpeakerName = evt.Speaker?.DisplayName,
                SpeakerRoleID = null,
                DialogueText = evt.AssembledText
            };
            
            // Clear the pending transition
            _pendingBeatTransition = null;
            
            AddEntry(entry);
        }

        /// <summary>
        /// Adds a new timeline entry to the list of timeline entries, updates the associated
        /// visual elements, and optionally scrolls to the latest entry in the timeline
        /// </summary>
        /// <param name="entry">
        /// The timeline entry to be added; this entry encapsulates data such as beat ID, index,
        /// speaker information, and any associated diagnostic events
        /// </param>
        private void AddEntry(TimelineEntry entry)
        {
            _entries.Add(entry);

            // Hide empty label
            _emptyLabel.style.display = DisplayStyle.None;

            // Create node view
            TimelineNodeView nodeView = new TimelineNodeView(entry);
            nodeView.OnSelected += OnNodeSelected;
            _nodeViews.Add(nodeView);
            _timelineGraph.Add(nodeView);

            // Auto-scroll disabled
            if (!_autoScroll) return;
            
            // Auto-scroll to latest
            EditorApplication.delayCall += () =>
            {
                // Exit case - no timeline scroll view exists
                if (_timelineScrollView == null) return;
                    
                // Scroll to the bottom
                _timelineScrollView.scrollOffset = new Vector2(
                    float.MaxValue,
                    _timelineScrollView.scrollOffset.y
                );
            };
        }

        /// <summary>
        /// Updates the visual representation of a timeline node associated with the provided timeline entry;
        /// this method ensures that the corresponding node view is refreshed to reflect the latest state or content of the entry
        /// </summary>
        /// <param name="entry">
        /// The timeline entry whose associated node view needs to be updated; this entry contains the latest
        /// data to be reflected in the visual representation
        /// </param>
        private void UpdateNodeView(TimelineEntry entry)
        {
            foreach (TimelineNodeView nodeView in _nodeViews)
            {
                // Skip if the entry does not match the node view's entry
                if (nodeView.Entry != entry) continue;
                
                nodeView.UpdateContent();
                break;
            }
        }

        /// <summary>
        /// Handles the selection of a timeline node; this method updates the internal state
        /// to reflect the newly selected node, deselects the previously selected node, and
        /// triggers an update to the detail panel to display information related to the newly selected entry
        /// </summary>
        /// <param name="nodeView">
        /// The timeline node view that has been selected; this parameter provides access to
        /// the associated timeline entry and allows the new selection to be visually and logically marked
        /// </param>
        private void OnNodeSelected(TimelineNodeView nodeView)
        {
            // Deselect previous
            _selectedNodeView?.SetSelected(false);

            // Select new
            _selectedNodeView = nodeView;
            _selectedEntry = nodeView.Entry;
            nodeView.SetSelected(true);

            UpdateDetailPanel();
        }

        /// <summary>
        /// Updates the detail panel UI to display information related to the currently selected
        /// timeline entry; this method refreshes the content of the detail panel by clearing existing
        /// elements and populates it with relevant data, such as beat information, dialogue text, scoring
        /// breakdowns, and selection strategy details corresponding to the selected timeline entry
        /// </summary>
        private void UpdateDetailPanel()
        {
            _detailScrollView.Clear();

            // Exit case - no entry selected
            if (_selectedEntry == null)
            {
                Label placeholder = new Label("Select anode to view details");
                placeholder.AddToClassList("detail-placeholder");
                _detailScrollView.Add(placeholder);
                return;
            }

            // Beat info section
            VisualElement beatSection = new VisualElement();
            beatSection.AddToClassList("detail-section");

            Label beatHeader = new Label("Beat Information");
            beatHeader.AddToClassList("detail-section-header");
            beatSection.Add(beatHeader);

            StringBuilder indexBuilder = new StringBuilder();
            indexBuilder.Append("#");
            indexBuilder.Append(_selectedEntry.Index + 1);
            
            beatSection.Add(CreateDetailRow("Beat ID", _selectedEntry.BeatID));
            beatSection.Add(CreateDetailRow("Timestamp", _selectedEntry.Timestamp.ToString("HH:mm:ss.fff")));
            beatSection.Add(CreateDetailRow("Index", indexBuilder.ToString()));

            if (!string.IsNullOrEmpty(_selectedEntry.SpeakerName))
                beatSection.Add(CreateDetailRow("Speaker", _selectedEntry.SpeakerName));

            if (_selectedEntry.BeatTransition != null)
            {
                beatSection.Add(CreateDetailRow("Scene", _selectedEntry.BeatTransition.SceneID));
                beatSection.Add(CreateDetailRow("From Beat", _selectedEntry.BeatTransition.FromBeatID ?? "—"));
                beatSection.Add(CreateDetailRow("To Beat", _selectedEntry.BeatTransition.ToBeatID ?? "(End)"));
            }

            // Dialogue text
            if (!string.IsNullOrEmpty(_selectedEntry.DialogueText))
            {
                Label textLabel = new Label(_selectedEntry.DialogueText);
                textLabel.AddToClassList("detail-dialogue-text");
                beatSection.Add(textLabel);
            }

            _detailScrollView.Add(beatSection);

            // Branch evaluation section
            if (_selectedEntry.BeatTransition != null)
            {
                BranchBreakdownView branchView = new BranchBreakdownView(
                    _selectedEntry.BeatTransition.BranchEvaluations, 
                    _selectedEntry.BeatTransition.UsedDefaultFallback
                );
                _detailScrollView.Add(branchView);
            }

            // Exit case - no fragment selected
            if (_selectedEntry.FragmentSelection == null) return;
            
            // Scoring breakdown section
            ScoringBreakdownView scoringView = new ScoringBreakdownView(_selectedEntry.FragmentSelection.AllCandidates);
            _detailScrollView.Add(scoringView);

            // Selection info section
            VisualElement selectionSection = new VisualElement();
            selectionSection.AddToClassList("detail-section");

            Label selectionHeader = new Label("Selection Info");
            selectionHeader.AddToClassList("detail-section-header");
            selectionSection.Add(selectionHeader);

            selectionSection.Add(CreateDetailRow(
                "Strategy", 
                _selectedEntry.FragmentSelection.SelectionStrategyUsed ?? "—"
            ));

            selectionSection.Add(CreateDetailRow(
                "Selected", 
                _selectedEntry.FragmentSelection.SelectedFragmentID ?? "None"
            ));
  
            selectionSection.Add(CreateDetailRow(
                "Variation Set", 
                _selectedEntry.FragmentSelection.VariationSetID ?? "—")
            );

            _detailScrollView.Add(selectionSection);
        }

        /// <summary>
        /// Creates a new detail row in the UI with a label and its associated value;
        /// this method is used for displaying information about a selected entry
        /// within the detail panels, where each row represents a single piece of data
        /// </summary>
        /// <param name="label">The text label representing the name of the data field to display</param>
        /// <param name="value">The value corresponding to the specified label; displayed as part of the row</param>
        /// <returns>A VisualElement representing the constructed detail row, with label and value</returns>
        private VisualElement CreateDetailRow(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("detail-row");

            Label labelElement = new Label(label + ":");
            labelElement.AddToClassList("detail-row-label");
            row.Add(labelElement);

            Label valueElement = new Label(value ?? "—");
            valueElement.AddToClassList("detail-row-value");
            row.Add(valueElement);

            return row;
        }

        /// <summary>
        /// Clears the timeline data and resets the UI components to their default state;
        /// this method is responsible for removing all timeline entries, node views, and
        /// clearing the associated graph and UI components related to the timeline;
        /// additionally, it restores placeholders and resets selected elements to null
        /// </summary>
        private void ClearTimeline()
        {
            // Clear all entries and node views
            _entries.Clear();
            _nodeViews.Clear();
            _selectedEntry = null;
            _selectedNodeView = null;

            // Clear timeline graph if it exists
            if (_timelineGraph != null)
            {
                _timelineGraph.Clear();
                _timelineGraph.Add(_emptyLabel);
                _emptyLabel.style.display = DisplayStyle.Flex;
            }

            // Exit case - no detail scroll view exists
            if (_detailScrollView == null) return;
            
            _detailScrollView.Clear();
                
            Label placeholder = new Label("Select a node to view details");
            placeholder.AddToClassList("detail-placeholder");
            _detailScrollView.Add(placeholder);
        }
    }
}
