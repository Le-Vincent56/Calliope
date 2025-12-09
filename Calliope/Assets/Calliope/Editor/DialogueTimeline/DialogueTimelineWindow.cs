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
        private TimelineEntry _selectedEntry;

        private bool _autoScroll = true;
        private bool _isConnected;
        private DiagnosticBeatTransitionEvent _pendingBeatTransition;

        private const int MaxVisibleNodes = 50;
        private const float UpdateIntervalSeconds = 0.1f;
        private readonly Queue<DiagnosticFragmentSelectionEvent> _pendingFragmentEvents = new Queue<DiagnosticFragmentSelectionEvent>();
        private readonly Queue<DiagnosticBeatTransitionEvent> _pendingBeatEvents = new Queue<DiagnosticBeatTransitionEvent>();
        private double _lastUpdateTime;
        private bool _updateSubscribed;
        
        private Label _statusLabel;
        private ListView _timelineListView;
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
            
            if(_timelineListView != null)
                _timelineListView.selectionChanged -= OnListSelectionChanged;
            
            UnscheduleUpdate();
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

            // Timeline list view
            _timelineListView = new ListView
            {
                fixedItemHeight = 80,
                itemsSource = _entries,
                makeItem = MakeTimelineItem,
                bindItem = BindTimelineItem,
                selectionType = SelectionType.Single
            };
            _timelineListView.AddToClassList("timeline-list-view");
            _timelineListView.selectionChanged += OnListSelectionChanged;
            
            _emptyLabel = new Label("Enter Play Mode and run a scene to see the dialogue timeline");
            _emptyLabel.AddToClassList("timeline-empty-text");

            VisualElement timelineContainer = new VisualElement();
            timelineContainer.AddToClassList("timeline-scroll-view");
            timelineContainer.Add(_emptyLabel);
            timelineContainer.Add(_timelineListView);
            content.Add(timelineContainer);
            
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
        /// Creates and configures a new VisualElement to represent a timeline item within the Dialogue Timeline window;
        /// the timeline item includes labels for index, beat, speaker, text, and timestamp, styled with appropriate class lists
        /// </summary>
        /// <returns>A VisualElement representing the configured timeline item, including its child elements</returns>
        private VisualElement MakeTimelineItem()
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("timeline-list-item");

            Label indexLabel = new Label();
            indexLabel.name = "index";
            indexLabel.AddToClassList("timeline-item-index");
            item.Add(indexLabel);

            VisualElement mainContent = new VisualElement();
            mainContent.AddToClassList("timeline-item-content");

            Label beatLabel = new Label();
            beatLabel.name = "beat";
            beatLabel.AddToClassList("timeline-item-beat");
            mainContent.Add(beatLabel);

            Label speakerLabel = new Label();
            speakerLabel.name = "speaker";
            speakerLabel.AddToClassList("timeline-item-speaker");
            mainContent.Add(speakerLabel);

            Label textLabel = new Label();
            textLabel.name = "text";
            textLabel.AddToClassList("timeline-item-text");
            mainContent.Add(textLabel);

            item.Add(mainContent);

            Label timestampLabel = new Label();
            timestampLabel.name = "timestamp";
            timestampLabel.AddToClassList("timeline-item-timestamp");
            item.Add(timestampLabel);
            
            // Cache references in userData for fast access during binding
            item.userData = new TimelineItemElements
            {
                Root = item,
                Index = indexLabel,
                Beat = beatLabel,
                Speaker = speakerLabel,
                Text = textLabel,
                Timestamp = timestampLabel
            };

            return item;
        }

        /// <summary>
        /// Binds a timeline entry to a specified UI element based on its index;
        /// updates the UI element's text content and styles based on the data
        /// from the timeline entry at the given index
        /// </summary>
        /// <param name="element">The UI element to bind data to</param>
        /// <param name="index">The index of the timeline entry to bind; must be within the valid range of timeline entries</param>
        private void BindTimelineItem(VisualElement element, int index)
        {
            // Exit case - the index is out of range
            if (index < 0 || index >= _entries.Count) return;

            // Exit case - no/incorrect cached data found
            if (element.userData is not TimelineItemElements elements) return;
            
            TimelineEntry entry = _entries[index];
            
            StringBuilder indexBuilder = new StringBuilder();
            indexBuilder.Append("#");
            indexBuilder.Append(index + 1);
            
            elements.Index.text = indexBuilder.ToString();
            elements.Beat.text = entry.BeatID ?? "Unknown";
            elements.Speaker.text = entry.SpeakerName ?? "(No speaker)";
            elements.Text.text = TruncateText(entry.DialogueText, 80);
            elements.Timestamp.text = entry.Timestamp.ToString("HH:mm:ss");

            // Style based on position
            element.RemoveFromClassList("timeline-item-start");
            element.RemoveFromClassList("timeline-item-end");

            // Style the element if it's the start or end of the timeline'
            if (index == 0)
                element.AddToClassList("timeline-item-start");
            if (entry.BeatTransition?.WasEndBeat == true)
                element.AddToClassList("timeline-item-end");
        }

        /// <summary>
        /// Handles changes in the selected items of a list, updating the internal state
        /// to reflect the first selected entry and refreshing the detail panel accordingly
        /// </summary>
        /// <param name="selection">An enumerable collection of objects representing the current selection in the list</param>
        private void OnListSelectionChanged(IEnumerable<object> selection)
        {
            _selectedEntry = null;

            foreach (object item in selection)
            {
                _selectedEntry = item as TimelineEntry;
                break;
            }

            UpdateDetailPanel();
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
            _pendingBeatEvents.Enqueue(evt);
            ScheduleUpdate();
        }

        /// <summary>
        /// Handles the event triggered when a dialogue fragment is selected within the Calliope Dialogue system;
        /// processes a <see cref="DiagnosticFragmentSelectionEvent"/> by queuing it for further updates
        /// and initiating necessary UI scheduling within the Dialogue Timeline window
        /// </summary>
        /// <param name="evt">
        /// The <see cref="DiagnosticFragmentSelectionEvent"/> containing information
        /// about the selected dialogue fragment, such as its ID, speaker, target, and selection details
        /// </param>
        private void OnFragmentSelection(DiagnosticFragmentSelectionEvent evt)
        {
            _pendingFragmentEvents.Enqueue(evt);
            ScheduleUpdate();
        }

        /// <summary>
        /// Ensures that the method responsible for processing pending events
        /// is subscribed to Unity's Editor update cycle; if already subscribed,
        /// no action is taken; this is used to defer or schedule processing
        /// of queued events such as beat transitions and fragment selections
        /// </summary>
        private void ScheduleUpdate()
        {
            // Exit case - processing is already subscribed to Editor updates
            if (_updateSubscribed) return;

            _updateSubscribed = true;
            EditorApplication.update += ProcessPendingEvents;
        }

        /// <summary>
        /// Unsubscribes the editor update callback to stop processing pending events
        /// for the Dialogue Timeline window, ensuring that unnecessary updates
        /// are avoided when they are no longer required, thereby improving
        /// performance and resource usage
        /// </summary>
        private void UnscheduleUpdate()
        {
            // Exit case - the processing is already unsubscribed from Editor updates
            if (!_updateSubscribed) return;

            _updateSubscribed = false;
            EditorApplication.update -= ProcessPendingEvents;
        }

        /// <summary>
        /// Processes any pending diagnostic events for the dialogue timeline
        /// by handling beat transitions and fragment selections, ensuring that
        /// the timeline remains up-to-date and relevant within the Unity Editor;
        /// also manages throttled updates and auto-scroll behavior for improved
        /// user experience when visualizing ongoing events
        /// </summary>
        private void ProcessPendingEvents()
        {
            // Exit case - not in play mode or not connected
            if (!EditorApplication.isPlaying || !_isConnected)
            {
                UnscheduleUpdate();
                return;
            }

            // Exit case - no pending events
            if (_pendingFragmentEvents.Count == 0 && _pendingBeatEvents.Count == 0)
            {
                UnscheduleUpdate();
                return;
            }

            // Throttle updates
            double currentTime = EditorApplication.timeSinceStartup;
            
            // Exit case - not enough time has elapsed since the last update
            if (currentTime - _lastUpdateTime < UpdateIntervalSeconds) return;
            
            _lastUpdateTime = currentTime;

            // Process all pending beat transitions first (store the latest for pairing)
            DiagnosticBeatTransitionEvent latestBeatTransition = _pendingBeatTransition;
            while (_pendingBeatEvents.Count > 0)
            {
                latestBeatTransition = _pendingBeatEvents.Dequeue();
            }
            _pendingBeatTransition = latestBeatTransition;

            // Process all pending fragment selections
            int addedCount = 0;
            while (_pendingFragmentEvents.Count > 0)
            {
                DiagnosticFragmentSelectionEvent evt = _pendingFragmentEvents.Dequeue();
                ProcessFragmentSelection(evt);
                addedCount++;
            }

            // Trim old nodes if we exceeded the limit
            TrimOldNodes();

            // Auto-scroll once after all additions (not per-node)
            if (addedCount > 0 && _autoScroll)
            {
                ScrollToEnd();
            }

            // Unsubscribe since we processed everything
            UnscheduleUpdate();
        }

        /// <summary>
        /// Processes a diagnostic event describing the selection of a specific dialogue fragment,
        /// using the provided event data to create and configure a new timeline entry; the method assigns
        /// fragment details, speaker information, and any pending beat transitions to the entry
        /// </summary>
        /// <param name="evt">
        /// The diagnostic event related to the fragment selection, containing metadata
        /// such as the speaker, beat ID, and assembled dialogue text
        /// </param>
        private void ProcessFragmentSelection(DiagnosticFragmentSelectionEvent evt)
        {
            string beatID = _pendingBeatTransition?.ToBeatID
                            ?? _pendingBeatTransition?.FromBeatID
                            ?? evt.BeatID
                            ?? "Unknown";

            TimelineEntry entry = new TimelineEntry(beatID)
            {
                Index = _entries.Count,
                BeatTransition = _pendingBeatTransition,
                FragmentSelection = evt,
                SpeakerName = evt.Speaker?.DisplayName,
                SpeakerRoleID = null,
                DialogueText = evt.AssembledText
            };

            _pendingBeatTransition = null;
            AddEntryInternal(entry);
        }

        /// <summary>
        /// Removes the oldest timeline nodes and their associated UI elements when the total number
        /// of nodes exceeds the configured maximum limit; this ensures the timeline remains within
        /// a manageable size for better performance and user experience; updates the indices
        /// of remaining entries to reflect their current order
        /// </summary>
        private void TrimOldNodes()
        {
            while (_entries.Count > MaxVisibleNodes)
            {
                _entries.RemoveAt(0);
            }

            // Update indices on remaining entries
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Index = i;
            }
        }

        /// <summary>
        /// Automatically scrolls the timeline to ensure the last node in the view is visible;
        /// utilizes a scheduling mechanism to wait for layout completion before adjusting
        /// the scroll position, ensuring accurate placement of the view at the timeline's end;
        /// if the scroll view or node list is empty, this method exits without performing any operation
        /// </summary>
        private void ScrollToEnd()
        {
            // Exit case - no timeline or entries
            if (_timelineListView == null || _entries.Count == 0) return;

            _timelineListView.ScrollToItem(_entries.Count - 1);
        }

        /// <summary>
        /// Adds a new timeline entry to the internal collection, updates the UI elements
        /// to reflect the addition, and hides the empty state indicator if it is visible
        /// </summary>
        /// <param name="entry">
        /// The timeline entry to be added, containing information such as
        /// the beat ID, speaker details, dialogue text, and associated transitions or selections
        /// </param>
        private void AddEntryInternal(TimelineEntry entry)
        {
            _entries.Add(entry);
            _emptyLabel.style.display = DisplayStyle.None;

            // Refresh the list view
            _timelineListView.RefreshItems();
        }

        /// <summary>
        /// Updates the detail panel UI to display information related to the currently selected
        /// timeline entry; this method refreshes the content of the detail panel by clearing existing
        /// elements and populates it with relevant data, such as beat information, dialogue text, scoring
        /// breakdowns, and selection strategy details corresponding to the selected timeline entry
        /// </summary>
        private void UpdateDetailPanel()
        {
            // Exit case - active playback happening
            if (_pendingFragmentEvents.Count > 0 || _pendingBeatEvents.Count > 0) 
                return;
            
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
            // Clear all event data
            _pendingFragmentEvents.Clear();
            _pendingBeatEvents.Clear();
            _pendingBeatTransition = null;
            UnscheduleUpdate();

            // Clear timeline graph if it exists
            _entries.Clear();
            _selectedEntry = null;
            _timelineListView?.RefreshItems();
            
            if(_emptyLabel != null)
                _emptyLabel.style.display = DisplayStyle.Flex;

            // Exit case - no detail scroll view exists
            if (_detailScrollView == null) return;
            
            _detailScrollView.Clear();
                
            Label placeholder = new Label("Select a node to view details");
            placeholder.AddToClassList("detail-placeholder");
            _detailScrollView.Add(placeholder);
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
