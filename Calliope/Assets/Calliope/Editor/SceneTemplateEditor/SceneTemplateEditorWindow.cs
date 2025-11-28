using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// A visual, node-based editor for creating and editing SceneTemplateSO assets
    /// </summary>
    public class SceneTemplateEditorWindow : EditorWindow
    {
        private SceneTemplateSO _currentTemplate;
        private VisualElement _graphView;
        private VisualElement _toolbar;
        private Button _createBeatButton;
        private Button _cleanupButton;
        private VisualElement _inspectorContent;
        private BeatNodeView _selectedNode;
        private ObjectField _templateField;
        private VisualElement _validationSection;
        private Button _validationButton;
        private BeatConnectionView _hoveredConnection;
        
        [MenuItem("Window/Calliope/Scene Template Editor")]
        public static void ShowWindow()
        {
            SceneTemplateEditorWindow window = GetWindow<SceneTemplateEditorWindow>();
            window.titleContent = new GUIContent("Calliope: Scene Template Editor");
            window.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            // Get the root visual element
            VisualElement root = rootVisualElement;
            
            // Load the UXML file
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Calliope/Editor/SceneTemplateEditor/SceneTemplateEditorWindow.uxml"
            );
            
            // Exit case - UXML file not found
            if (!visualTree)
            {
                Debug.LogError(
                    "[SceneTemplateEditorWindow] Failed to load UXML file at 'Assets/Calliope/Editor/SceneTemplateEditor/SceneTemplateEditorWindow.uxml'"
                );

                // Create a fallback UI
                CreateFallbackUI(root);
                return;
            }

            // Clone the UXML tree into the root
            visualTree.CloneTree(root);
            
            // Load the USS stylesheet
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Calliope/Editor/SceneTemplateEditor/SceneTemplateEditorWindow.uss"
            );
            
            // Check if a style sheet was found
            if (!styleSheet)
            {
                Debug.LogWarning(
                    "[SceneTemplateEditor] Could not find USS file at 'Assets/Calliope/Editor/SceneTemplateEditor/SceneTemplateEditorWindow.uss'"
                );
            }
            else
            {
                root.styleSheets.Add(styleSheet);
            }
            
            // Cache references to important UI elements
            _toolbar = root.Q<VisualElement>("Toolbar");
            _graphView = root.Q<VisualElement>("GraphView");
            _templateField = root.Q<ObjectField>("TemplateField");
            _createBeatButton = root.Q<Button>("CreateBeatButton");
            _cleanupButton = root.Q<Button>("CleanupButton");
            _inspectorContent = root.Q<VisualElement>("InspectorContent");
            _validationSection = root.Q<VisualElement>("ValidationSection");
            _validationButton = root.Q<Button>("ValidateButton");
            
            // Set up the create beat button
            if (_createBeatButton != null)
            {
                _createBeatButton.clicked += OnCreateBeatClicked;
            }
            
            // Set up the cleanup button
            if (_cleanupButton != null)
            {
                _cleanupButton.clicked += CleanupOrphanedBranches;
            }
            
            // Set up the validation button
            if (_validationButton != null)
            {
                _validationButton.clicked += ValidateScene;
            }

            // Set up the template selector
            if (_templateField != null)
            {
                // Configure the template field
                _templateField.objectType = typeof(SceneTemplateSO);
                _templateField.RegisterValueChangedCallback(OnTemplateChanged);
                
                // Set the current template if it exists
                if(_currentTemplate)
                    _templateField.value = _currentTemplate;
            }
            
            // Initialize the graph view
            InitializeGraphView();
        }

        /// <summary>
        /// Handles the event triggered when the "Create Beat" button is clicked;
        /// this method creates a new SceneBeatSO asset, assigns it a unique identifier, and adds it to the currently selected SceneTemplateSO asset;
        /// Updates the template, saves the changes, and logs the creation of the new beat
        /// </summary>
        private void OnCreateBeatClicked()
        {
            // Exit case - no template selected
            if (!_currentTemplate)
            {
                EditorUtility.DisplayDialog("No Template", "Please select a Scene Template first.", "OK");
                return;
            }
            
            // Show the dialog
            BeatCreationDialog.Show(
                onCreate: (beatID, displayName) => CreateBeat(beatID, displayName, null),
                onValidateID: IsBeatIDUnique
            );
        }

        private void CreateBeat(string beatID, string displayName, Vector2? nodePosition)
        {
            // Exit case - no template selected
            if(!_currentTemplate) return;
            
            // Create a new SceneBeatSO asset
            SceneBeatSO newBeat = CreateInstance<SceneBeatSO>();
            
            // Set the beat ID
            SerializedObject beatSerializedObject = new SerializedObject(newBeat);
            SerializedProperty beatIDProperty = beatSerializedObject.FindProperty("beatID");
            if (beatIDProperty != null)
            {
                beatIDProperty.stringValue = beatID;
                beatSerializedObject.ApplyModifiedProperties();
            }
            
            // Save the new beat as a sub-asset
            string templatePath = AssetDatabase.GetAssetPath(_currentTemplate);
            newBeat.name = string.IsNullOrEmpty(displayName) ? beatID : displayName;
            AssetDatabase.AddObjectToAsset(newBeat, templatePath);
            
            // Add the beat to the template's beats array
            SerializedObject serializedObject = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serializedObject.FindProperty("beats");
            if (beatsProperty != null)
            {
                beatsProperty.InsertArrayElementAtIndex(beatsProperty.arraySize);
                SerializedProperty newElement = beatsProperty.GetArrayElementAtIndex(beatsProperty.arraySize - 1);
                newElement.objectReferenceValue = newBeat;
                serializedObject.ApplyModifiedProperties();
            }
            
            // Save assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Save the position if provided
            if (nodePosition.HasValue)
            {
                NodePositionStorage.SavePosition(_currentTemplate.ID, beatID, nodePosition.Value);
            }

            // Refresh the graph view
            InitializeGraphView();
        }

        /// <summary>
        /// Verifies whether the provided beat ID is unique within the beats array of the currently selected SceneTemplateSO
        /// </summary>
        /// <param name="beatID">The beat ID to validate for uniqueness</param>
        /// <returns>
        /// True if the beat ID is unique (not present in the current template's beats array), otherwise false
        /// </returns>
        private bool IsBeatIDUnique(string beatID)
        {
            SerializedObject serializedObject = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serializedObject.FindProperty("beats");

            // Exit case - no beats property found
            if (beatsProperty == null) return true;

            for (int i = 0; i < beatsProperty.arraySize; i++)
            {
                SerializedProperty beatProperty = beatsProperty.GetArrayElementAtIndex(i);
                SceneBeatSO beat = beatProperty.objectReferenceValue as SceneBeatSO;

                // Skip if the beat does not exist or is mismatching
                if (!beat || beat.BeatID != beatID) continue;
                
                return false;
            }

            return true;
        }

        /// <summary>
        /// Displays the beat inspector in the editor window for the specified BeatNodeView;
        /// this method dynamically populates the inspector content with relevant fields for editing
        /// properties of the selected SceneBeatSO instance or shows a placeholder message if no beat is selected;
        /// includes functionality for property editing and deleting the beat
        /// </summary>
        /// <param name="nodeView">
        /// The BeatNodeView instance representing the selected beat;
        /// provides access to the SceneBeatSO being inspected and edited
        /// </param>
        private void ShowBeatInspector(BeatNodeView nodeView)
        {
            // Store the selected node
            _selectedNode = nodeView;
            
            // Clear the inspector
            _inspectorContent.Clear();
            
            // Exit case - no node selected
            if (nodeView == null || !nodeView.Beat)
            {
                Label placeholder = new Label("Select a beat to edit");
                placeholder.AddToClassList("inspector-placeholder");
                _inspectorContent.Add(placeholder);
                return;
            }
            
            SceneBeatSO beat = nodeView.Beat;
            StringBuilder labelBuilder = new StringBuilder();
            
            // Create inspector fields
            labelBuilder.Append("Editing: ");
            labelBuilder.Append(beat.BeatID);
            Label titleLabel = new Label(labelBuilder.ToString());
            titleLabel.style.fontSize = 16;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 12;
            _inspectorContent.Add(titleLabel);
            
            // Beat ID field
            TextField beatIDField = new TextField("Beat ID");
            beatIDField.value = beat.BeatID ?? "";
            beatIDField.style.marginBottom = 8;
            beatIDField.RegisterValueChangedCallback(evt => OnBeatPropertyChanged(beat, "beatID", evt.newValue));
            _inspectorContent.Add(beatIDField);
            
            // Speaker role field
            SearchableDropdown speakerDropdown = new SearchableDropdown(
                label: "Speaker Role",
                prefsKey: "SpeakerRole",
                onGetItems: GetRoleDropdownItems,
                onValueChanged: (value) => OnBeatPropertyChanged(beat, "speakerRoleID", value),
                allowCreateNew: true,
                onCreateNew: (searchText) => CreateNewRole(searchText, (roleID) =>
                {
                    OnBeatPropertyChanged(beat, "speakerRoleID", roleID);
                    ShowBeatInspector(nodeView);
                })
            );
            speakerDropdown.Value = beat.SpeakerRoleID ?? "";
            _inspectorContent.Add(speakerDropdown);
            
            // Target role field
            SearchableDropdown targetDropdown = new SearchableDropdown(
                label: "Target Role",
                prefsKey: "TargetRole",
                onGetItems: GetRoleDropdownItems,
                onValueChanged: (value) => OnBeatPropertyChanged(beat, "targetRoleID", value),
                allowCreateNew: true,
                onCreateNew: (searchText) => CreateNewRole(searchText, (roleID) =>
                {
                    OnBeatPropertyChanged(beat, "targetRoleID", roleID);
                    ShowBeatInspector(nodeView);
                })
            );
            targetDropdown.Value = beat.TargetRoleID ?? "";
            _inspectorContent.Add(targetDropdown);
            
            // Variation Set field
            SearchableDropdown variationDropdown = new SearchableDropdown(
                label: "Variation Set",
                prefsKey: "VariationSet",
                onGetItems: GetVariationSetDropdownItems,
                onValueChanged: (value) => OnBeatPropertyChanged(beat, "VariationSetID", value),
                allowCreateNew: true,
                onCreateNew: (searchText) => CreateNewVariationSet(searchText, (variationSetID) =>
                {
                    OnBeatPropertyChanged(beat, "VariationSetID", variationSetID);
                    ShowBeatInspector(nodeView);
                })
            );
            variationDropdown.Value = beat.VariationSetID ?? "";
            _inspectorContent.Add(variationDropdown);
            
            // Spacer
            VisualElement spacer = new VisualElement();
            spacer.style.height = 16;
            _inspectorContent.Add(spacer);
            
            // Delete button
            Button deleteButton = new Button(() => OnDeleteButtonClicked(nodeView));
            deleteButton.text = "Delete Beat";
            deleteButton.style.marginTop = 16;
            deleteButton.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            _inspectorContent.Add(deleteButton);
        }

        /// <summary>
        /// Retrieves a list of dropdown items representing available roles for a SceneTemplate;
        /// this method scans the assets for SceneRoleSO instances, skips any invalid roles,
        /// and constructs a dropdown item for each valid role based on its ID and display name
        /// </summary>
        /// <returns>
        /// A list of SearchableDropdown.DropdownItem objects, each corresponding to a valid SceneRoleSO asset
        /// </returns>
        private List<SearchableDropdown.DropdownItem> GetRoleDropdownItems()
        {
            List<SearchableDropdown.DropdownItem> items = new List<SearchableDropdown.DropdownItem>();
            
            // Find all SceneRoleSO assets based on the current scan mode
            List<SceneRoleSO> roles = AssetCreator.FindAllAssets<SceneRoleSO>();

            // Add items to the dropdown
            for (int i = 0; i < roles.Count; i++)
            {
                SceneRoleSO role = roles[i];

                // Skip if the role does not exist
                if (!role) continue;
                
                // Skip if the role ID does not exist
                if (string.IsNullOrEmpty(role.RoleID)) continue;
                
                items.Add(new SearchableDropdown.DropdownItem(
                    id: role.RoleID, 
                    displayName: role.DisplayName ?? role.RoleID
                ));
            }

            return items;
        }

        /// <summary>
        /// Retrieves a list of dropdown items for the variation set selection in the Scene Template Editor;
        /// scans for available VariationSetSO assets, processes them, and creates dropdown items using their identifiers and display names,
        /// while skipping invalid or incomplete entries
        /// </summary>
        /// <returns>A list of dropdown items derived from available VariationSetSO assets</returns>
        private List<SearchableDropdown.DropdownItem> GetVariationSetDropdownItems()
        {
            List<SearchableDropdown.DropdownItem> items = new List<SearchableDropdown.DropdownItem>();
            
            // Find all VariationSetSO assets based on the current scan mode
            List<VariationSetSO> variationSets = AssetCreator.FindAllAssets<VariationSetSO>();
            
            // Add items to the dropdown
            for (int i = 0; i < variationSets.Count; i++)
            {
                VariationSetSO variationSet = variationSets[i];

                // Skip if the variation set does not exist
                if (!variationSet) continue;

                // Skip if the variation set ID does not exist
                if (string.IsNullOrEmpty(variationSet.ID)) continue;
                
                items.Add(new SearchableDropdown.DropdownItem(
                    id: variationSet.ID,
                    displayName: variationSet.DisplayName ?? variationSet.ID
                ));
            }

            return items;
        }

        /// <summary>
        /// Creates a new SceneRoleSO asset with a specified name, saves it to a user-specified location,
        /// assigns initial values to its properties, and triggers a callback with the generated role ID
        /// </summary>
        /// <param name="suggestedName">The default name suggested for the new role asset</param>
        /// <param name="onRoleCreated">An action to be invoked with the generated role ID after the role is created and saved</param>
        private void CreateNewRole(string suggestedName, Action<string> onRoleCreated)
        {
            // Create the asset
            SceneRoleSO newRole = AssetCreator.CreateAsset<SceneRoleSO>(
                suggestedName,
                (role, serialized) =>
                {
                    // Set the initial values
                    string roleName = string.IsNullOrEmpty(suggestedName) ? "NewRole" : suggestedName;
                    string roleID = roleName.ToLower().Replace(" ", "-");

                    SerializedProperty roleIDProperty = serialized.FindProperty("roleID");
                    SerializedProperty displayNameProperty = serialized.FindProperty("displayName");

                    if (roleIDProperty != null)
                        roleIDProperty.stringValue = roleID;
                    if (displayNameProperty != null)
                        displayNameProperty.stringValue = roleName;
                }
            );

            // Exit case - the asset was not created
            if (!newRole) return;

            onRoleCreated?.Invoke(newRole.RoleID);
        }

        /// <summary>
        /// Creates a new Variation Set asset in the Unity project, prompts the user to select a save location,
        /// initializes the asset with a unique identifier and display name, saves it to the specified location,
        /// refreshes the AssetDatabase, and invokes a callback with the created Variation Set's identifier
        /// </summary>
        /// <param name="suggestedName">Suggested default name for the new Variation Set asset; used if not overridden by the user</param>
        /// <param name="onVariationSetCreated">
        /// Callback function invoked upon successfully creating the Variation Set,
        /// with the generated unique identifier for the Variation Set passed as a parameter
        /// </param>
        private void CreateNewVariationSet(string suggestedName, Action<string> onVariationSetCreated)
        {
            // Create the asset
            VariationSetSO newVariationSet = AssetCreator.CreateAsset<VariationSetSO>(
                suggestedName,
                (variationSet, serialized) =>
                {
                    string variationSetName = string.IsNullOrEmpty(suggestedName) ? "NewVariationSet" : suggestedName;
                    string variationSetID = variationSetName.ToLower().Replace(" ", "-");
            
                    SerializedProperty variationSetIDProperty = serialized.FindProperty("id");
                    SerializedProperty displayNameProperty = serialized.FindProperty("displayName");
            
                    if (variationSetIDProperty != null) 
                        variationSetIDProperty.stringValue = variationSetID;
                    if (displayNameProperty != null) 
                        displayNameProperty.stringValue = variationSetName;
                }
            );

            // Exit case - the asset was not created
            if (!newVariationSet) return;
            
            onVariationSetCreated?.Invoke(newVariationSet.ID);
        }

        /// <summary>
        /// Updates a specified property of a SceneBeatSO asset with a new value;
        /// if the property exists, its value is updated, the changes are applied,
        /// the asset marked as dirty, and saved to reflect the modification;
        /// if the property does not exist, logs a warning indicating the missing property;
        /// also triggers a refresh of the graph view to reflect changes in the editor
        /// </summary>
        /// <param name="beat">The SceneBeatSO asset whose property is to be updated</param>
        /// <param name="propertyName">The name of the property to be updated</param>
        /// <param name="newValue">The new value to set for the specified property</param>
        private void OnBeatPropertyChanged(SceneBeatSO beat, string propertyName, string newValue)
        {
            // Get the property value
            SerializedObject serializedObject = new SerializedObject(beat);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            // Exit case - the property doesn't exist
            if (property == null)
            {
                StringBuilder warningBuilder = new StringBuilder();
                warningBuilder.Append("[SceneTemplateEditor] Could not find property '");
                warningBuilder.Append(propertyName);
                warningBuilder.Append("' on beat '");
                warningBuilder.Append(beat.BeatID);
                warningBuilder.Append("'");
                
                Debug.LogWarning(warningBuilder.ToString());
                return;
            }
            
            // Set the property value
            property.stringValue = newValue;
            serializedObject.ApplyModifiedProperties();
                
            // Mark the editor as dirty and save assets
            EditorUtility.SetDirty(beat);
            AssetDatabase.SaveAssets();
                
            // Refresh the node view
            InitializeGraphView();
        }

        /// <summary>
        /// Handles the deletion of a beat associated with the provided BeatNodeView instance;
        /// prompts the user for confirmation, removes the selected beat from the current SceneTemplateSO asset,
        /// deletes the corresponding asset from the project, saves changes, and refreshes the editor view
        /// </summary>
        /// <param name="nodeView">The BeatNodeView instance representing the beat to delete</param>
        private void OnDeleteButtonClicked(BeatNodeView nodeView)
        {
            // Exit case - no template or beat
            if (!_currentTemplate || !nodeView.Beat)
                return;
            
            StringBuilder confirmBuilder = new StringBuilder();
            confirmBuilder.Append("Are you sure you want to delete beat '");
            confirmBuilder.Append(nodeView.Beat.BeatID);
            confirmBuilder.Append("'?");
            
            // Confirm deletion
            if (!EditorUtility.DisplayDialog(
                    "Delete Beat",
                    confirmBuilder.ToString(),
                    "Delete",
                    "Cancel"
                )
            ) return;

            // Get the beat to delete
            SceneBeatSO beatToDelete = nodeView.Beat;
            
            // Get the template to delete the beat from
            SerializedObject serializedObject = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serializedObject.FindProperty("beats");

            // Remove from the template's beats array
            if (beatsProperty != null)
            {
                for (int i = 0; i < beatsProperty.arraySize; i++)
                {
                    SerializedProperty element = beatsProperty.GetArrayElementAtIndex(i);
                    
                    // Skip if the element does not reference the beat to delete
                    if (element.objectReferenceValue != beatToDelete) continue;
                    
                    beatsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                // Save changes
                serializedObject.ApplyModifiedProperties();
            }
            
            // Remove the asset
            AssetDatabase.RemoveObjectFromAsset(beatToDelete);
            DestroyImmediate(beatToDelete, true);
            AssetDatabase.SaveAssets();

            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("[SceneTemplateEditor] Delete beated '");
            debugBuilder.Append(beatToDelete.BeatID);
            debugBuilder.Append("'");
            
            // Refresh the view
            InitializeGraphView();
        }

        /// <summary>
        /// Creates a fallback UI for the Scene Template Editor Window when the UXML file cannot be loaded;
        /// displays an error message to the user
        /// </summary>
        /// <param name="root">The root VisualElement to which the fallback UI will be added</param>
        private void CreateFallbackUI(VisualElement root)
        {
            // Construct the error label
            Label errorLabel = new Label("Could not load Scene Template Editor UI\n. Make sure the UXML file exists.");
            errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            errorLabel.style.fontSize = 16;
            errorLabel.style.color = Color.red;
            errorLabel.style.flexGrow = 1;
            
            root.Add(errorLabel);
        }

        /// <summary>
        /// Initializes the graph view for the Scene Template Editor window.
        /// Clears any existing content in the graph view and populates it with nodes if a Scene Template is selected;
        /// displays a placeholder message in the graph view when no Scene Template is selected
        /// </summary>
        private void InitializeGraphView()
        {
            // Exit case - the graph view was not found
            if (_graphView == null)
            {
                Debug.LogError("[SceneTemplateEditor] GraphView element not found in UXML");
                return;
            }
            
            // Clear any existing content
            _graphView.Clear();
            
            // Register mouse move handler for connection hover
            _graphView.RegisterCallback<MouseMoveEvent>(OnGraphViewMouseMove);
            
            // Exit case - no template is selected
            if (!_currentTemplate)
            {
                // Show a placeholder message
                Label placeholderLabel = new Label("Select a Scene Template to begin editing");
                placeholderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                placeholderLabel.style.fontSize = 18;
                placeholderLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                placeholderLabel.style.flexGrow = 1;
                
                _graphView.Add(placeholderLabel);
                return;
            }

            // Create the nodes and connections for the template
            CreateBeatNodes();
            CreateBeatConnections();
        }

        /// <summary>
        /// Creates and populates beat nodes for the graphical editor of the Scene Template;
        /// nodes are positioned on the editor graph, representing the beats from the associated SceneTemplateSO
        /// </summary>
        private void CreateBeatNodes()
        {
            // Exit case - no template is selected
            if (!_currentTemplate) return;
            
            // Get the beats array from the ScriptableObject
            SerializedObject serializedObject = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serializedObject.FindProperty("beats");
            
            // Exit case - no beats property found
            if (beatsProperty == null)
            {
                Debug.LogWarning("[SceneTemplateEditor] Could not find 'beats' property on SceneTemplateSO");
                return;
            }
            
            // Exit case - no beats in the array
            if (beatsProperty.arraySize == 0)
            {
                // Construct UI to tell the user
                Label noBeatLabel = new Label(
                    "No beats in this scene template.\nAdd beats to the SceneTemplateSO to see them here."
                );
                noBeatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noBeatLabel.style.fontSize = 16;
                noBeatLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                noBeatLabel.style.flexGrow = 1;
                
                _graphView.Add(noBeatLabel);
                return;
            }
            
            // Create a node for each beat
            float nodeSpacing = 250f;
            float startX = 50f;
            float startY = 50f;

            for (int i = 0; i < beatsProperty.arraySize; i++)
            {
                SerializedProperty beatProperty = beatsProperty.GetArrayElementAtIndex(i);
                SceneBeatSO beat = beatProperty.objectReferenceValue as SceneBeatSO;
                
                // Skip null beats
                if (!beat) continue;
                
                // Calculate the position of the node
                int column = i % 3;
                int row = i / 3;
                Vector2 defaultPosition = new Vector2(
                    startX + (column * nodeSpacing),
                    startY + (row * 150f)
                );
                
                // Load save position (or use default)
                Vector2 nodePosition = NodePositionStorage.LoadPosition(
                    _currentTemplate.ID,
                    beat.BeatID,
                    defaultPosition
                );
                
                // Create the beat node view
                BeatNodeView beatNodeView = new BeatNodeView(beat, nodePosition, this);
                
                // Add the click handler for selection
                beatNodeView.RegisterCallback<MouseDownEvent>(evt => OnBeatNodeClicked(beatNodeView, evt));
                
                // Add connection drag manipulator to output port
                beatNodeView.OutputPort?.AddManipulator(new ConnectionDragManipulator(beatNodeView, this));

                _graphView.Add(beatNodeView);
            }
        }

        /// <summary>
        /// Establishes connections between beat nodes in the graph view based on the branching structure
        /// defined in the associated SceneTemplateSO asset; this method iterates through all beat nodes,
        /// maps their IDs, and connects nodes corresponding to branching relationships
        /// </summary>
        private void CreateBeatConnections()
        {
            // Exit case - no template is selected
            if (!_currentTemplate) return;
            
            // Get all beat nodes
            List<BeatNodeView> beatNodes = _graphView.Query<BeatNodeView>().ToList();
            
            // Create a lookup dictionary for quick access
            Dictionary<string, BeatNodeView> nodeLookup = new Dictionary<string, BeatNodeView>();

            for (int i = 0; i < beatNodes.Count; i++)
            {
                BeatNodeView node = beatNodes[i];

                // Skip if the node does not have a beat assigned
                if (!node.Beat) continue;
                
                // Skip if the beat ID is empty
                if(string.IsNullOrEmpty(node.Beat.BeatID)) continue;
                
                // Add the node to the lookup dictionary
                nodeLookup[node.Beat.BeatID] = node;
            }
            
            // Create connections for each beat's branches
            for (int i = 0; i < beatNodes.Count; i++)
            {
                BeatNodeView fromNode = beatNodes[i];
                SceneBeatSO beat = fromNode.Beat;
                
                // Skip if there is no beat or no branches
                if (!beat || beat.Branches == null || beat.Branches.Count == 0)
                    continue;
                
                // Create a connection for each branch
                for (int j = 0; j < beat.Branches.Count; j++)
                {
                    IBeatBranch branch = beat.Branches[j];
                    
                    // Skip invalid branches
                    if (branch == null || string.IsNullOrEmpty(branch.NextBeatID))
                        continue;
                    
                    // Find the target node
                    if (nodeLookup.TryGetValue(branch.NextBeatID, out BeatNodeView toNode))
                    {
                        // Create the connection
                        BeatConnectionView connection = new BeatConnectionView(fromNode, toNode, branch, this);
                        
                        // Add it to the graph (before nodes so connections draw behind them)
                        _graphView.Insert(0, connection);
                    }
                    else
                    {
                        StringBuilder warningBuilder = new StringBuilder();
                        warningBuilder.Append("[SceneTemplateEditor] Branch target '");
                        warningBuilder.Append(branch.NextBeatID);
                        warningBuilder.Append("' not found for beat '");
                        warningBuilder.Append(beat.BeatID);
                        warningBuilder.Append("'");
                        Debug.LogWarning(warningBuilder.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Handles the click event on a beat node within the Scene Template Editor,
        /// enabling the selection of the node and preventing further propagation of the event
        /// </summary>
        /// <param name="nodeView">The BeatNodeView that represents the clicked beat node</param>
        /// <param name="evt">The MouseDownEvent triggered by the user's click action</param>
        private void OnBeatNodeClicked(BeatNodeView nodeView, MouseDownEvent evt)
        {
            // Select the node
            SelectNode(nodeView);
            evt.StopPropagation();
        }

        /// <summary>
        /// Selects the specified node in the graph view, updating its visual state to indicate selection
        /// while deselecting all other nodes
        /// </summary>
        /// <param name="selectedNode">The node to be selected in the graph view</param>
        private void SelectNode(BeatNodeView selectedNode)
        {
            // Deselect all nodes
            _graphView.Query<BeatNodeView>().ForEach(node => node.SetSelected(false));
            
            // Select the clicked node
            selectedNode.SetSelected(true);
            
            // Show the inspector for the selected node
            ShowBeatInspector(selectedNode);
        }

        /// <summary>
        /// Updates all active connections within the graph view by iterating through
        /// each instance of BeatConnectionView and invoking its UpdateConnection method;
        /// this ensures the visual representation of connections is refreshed to reflect
        /// changes in node layout or other editor modifications
        /// </summary>
        public void UpdateConnections()
        {
            _graphView.Query<BeatConnectionView>().ForEach(connection => connection.UpdateConnection());
        }

        /// <summary>
        /// Handles changes to the selected Scene Template in the ObjectField, updating the editor to reflect the new template
        /// </summary>
        /// <param name="event">The event containing information about the change, including the new and previous values of the ObjectField</param>
        private void OnTemplateChanged(ChangeEvent<Object> @event)
        {
            _currentTemplate = @event.newValue as SceneTemplateSO;
            InitializeGraphView();
        }

        public void SaveNodePositions()
        {
            // Exit case - no template is selected
            if (!_currentTemplate) return;
            
            // Save the position for each node
            _graphView.Query<BeatNodeView>().ForEach(nodeView =>
            {
                NodePositionStorage.SavePosition(
                    _currentTemplate.ID,
                    nodeView.Beat.BeatID,
                    nodeView.Position
                );
            });
        }

        /// <summary>
        /// Creates a branch between two beat nodes in the scene template graph;
        /// this involves verifying the nodes, creating a new branch as a ScriptableObject,
        /// associating it with the originating beat, saving it as a sub-asset of the current template,
        /// and updating all related assets; additionally, logs the action and refreshes the graph view
        /// </summary>
        /// <param name="fromNode">The starting node of the branch, representing the origin beat</param>
        /// <param name="toNode">The ending node of the branch, representing the target beat</param>
        public void CreateBranch(BeatNodeView fromNode, BeatNodeView toNode)
        {
            // Exit case - invalid nodes
            if (fromNode == null || toNode == null || !fromNode.Beat || !toNode.Beat)
                return;
            
            StringBuilder debugBuilder = new StringBuilder();
            
            // Check if the branch already exists
            if (fromNode.Beat.Branches != null)
            {
                for (int i = 0; i < fromNode.Beat.Branches.Count; i++)
                {
                    // Skip if the branch doesn't exist
                    if (fromNode.Beat.Branches[i] != null && fromNode.Beat.Branches[i].NextBeatID != toNode.Beat.BeatID)
                        continue;

                    // build the debug message
                    debugBuilder.Append("[SceneTemplateEditor] Branch already exists from beat '");
                    debugBuilder.Append(fromNode.Beat.BeatID);
                    debugBuilder.Append("' to beat '");
                    debugBuilder.Append(toNode.Beat.BeatID);
                    debugBuilder.Append("'");
                    
                    Debug.LogWarning(debugBuilder.ToString());
                    return;
                }
            }
            
            // Create a new branch ScriptableObject
            BeatBranchSO newBranch = CreateInstance<BeatBranchSO>();
            
            // Set the target beat ID
            SerializedObject branchSerializedObject = new SerializedObject(newBranch);
            SerializedProperty targetBeatIDProperty = branchSerializedObject.FindProperty("nextBeatID");
            if (targetBeatIDProperty != null)
            {
                targetBeatIDProperty.stringValue = toNode.Beat.BeatID;
                branchSerializedObject.ApplyModifiedProperties();
            }
            
            // Construct the file name
            StringBuilder nameBuilder = new StringBuilder();
            nameBuilder.Append("Branch_");
            nameBuilder.Append(fromNode.Beat.BeatID);
            nameBuilder.Append("_to_");
            nameBuilder.Append(toNode.Beat.BeatID);
            
            // Save as a sub-asset of the current template
            string templatePath = AssetDatabase.GetAssetPath(_currentTemplate);
            newBranch.name = nameBuilder.ToString();
            AssetDatabase.AddObjectToAsset(newBranch, templatePath);
            
            // Add the branch to the beat's branches array
            SerializedObject beatSerializedObject = new SerializedObject(fromNode.Beat);
            SerializedProperty branchesProperty = beatSerializedObject.FindProperty("branches");
            if (branchesProperty != null)
            {
                branchesProperty.InsertArrayElementAtIndex(branchesProperty.arraySize);
                SerializedProperty newElement = branchesProperty.GetArrayElementAtIndex(branchesProperty.arraySize - 1);
                newElement.objectReferenceValue = newBranch;
                beatSerializedObject.ApplyModifiedProperties();
            }
            
            // Save
            EditorUtility.SetDirty(fromNode.Beat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Log the branch creation
            debugBuilder.Append("[SceneTemplateEditor] Created branch from beat '");
            debugBuilder.Append(fromNode.Beat.BeatID);
            debugBuilder.Append("' to beat '");
            debugBuilder.Append(toNode.Beat.BeatID);
            debugBuilder.Append("'");
            Debug.Log(debugBuilder.ToString());
            
            // Refresh the graph view
            InitializeGraphView();
        }

        /// <summary>
        /// Deletes a branch connection between two beat nodes within the scene template;
        /// removes the connection from the source beat's branches, deletes the associated ScriptableObject,
        /// updates the assets, and refreshes the editor view
        /// </summary>
        /// <param name="fromNode">The source beat node from which the branch originates</param>
        /// <param name="toNode">The destination beat node to which the branch connects</param>
        /// <param name="branch">The branch object representing the connection to be deleted</param>
        public void DeleteBranch(BeatNodeView fromNode, BeatNodeView toNode, IBeatBranch branch)
        {
            // Exit case - invalid parameters
            if (fromNode == null || toNode == null || branch == null || !fromNode.Beat)
                return;
            
            // Cast to ScriptableObject for deletion
            if (branch is not ScriptableObject branchSO)
            {
                Debug.LogError("[SceneTemplateEditor] Branch is not a ScriptableObject");
                return;
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Delete connection from '");
            debugBuilder.Append(fromNode.Beat.BeatID);
            debugBuilder.Append("' to '");
            debugBuilder.Append(toNode.Beat.BeatID);
            debugBuilder.Append("'?");
            
            // Confirm deletion
            if (!EditorUtility.DisplayDialog(
                    "Delete Branch",
                    debugBuilder.ToString(),
                    "Delete",
                    "Cancel"
                )
            ) return;
            
            // Remove the beat's branches array
            SerializedObject beatSerializedObject = new SerializedObject(fromNode.Beat);
            SerializedProperty branchesProperty = beatSerializedObject.FindProperty("branches");

            if (branchesProperty != null)
            {
                for (int i = 0; i < branchesProperty.arraySize; i++)
                {
                    SerializedProperty element = branchesProperty.GetArrayElementAtIndex(i);

                    // Skip if the element does not reference the branch to delete
                    if (element.objectReferenceValue != branchSO) continue;
                    
                    // Remove the branch from the array
                    branchesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                beatSerializedObject.ApplyModifiedProperties();
            }
            
            // Remove the asset
            AssetDatabase.RemoveObjectFromAsset(branchSO);
            DestroyImmediate(branchSO, true);
            
            // Save
            EditorUtility.SetDirty(fromNode.Beat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Log the branch deletion
            StringBuilder builder = new StringBuilder();
            builder.Append("[SceneTemplateEditor] Deleted branch from beat '");
            builder.Append(fromNode.Beat.BeatID);
            builder.Append("' to beat '");
            builder.Append(toNode.Beat.BeatID);
            builder.Append("'");
            Debug.Log(builder.ToString());
            
            // Refresh view
            InitializeGraphView();
        }

        /// <summary>
        /// Cleans up orphaned branch assets that are no longer referenced by any beats in the currently selected SceneTemplateSO;
        /// Identifies orphaned branches, removes them from the asset database, and logs the results;
        /// Updates the asset database, refreshes the UI, and ensures the graph view reflects the changes
        /// </summary>
        private void CleanupOrphanedBranches()
        {
            // Exit case - no template selected
            if (_currentTemplate == null)
            {
                Debug.LogWarning("[SceneTemplateEditor] No template selected");
                return;
            }
            
            // Get all sub-assets
            string templatePath = AssetDatabase.GetAssetPath(_currentTemplate);
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(templatePath);
            
            // Collect all branch references from beats
            HashSet<Object> referencedBranches = new HashSet<Object>();
            SerializedObject serializedObject = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serializedObject.FindProperty("beats");

            if (beatsProperty != null)
            {
                for (int i = 0; i < beatsProperty.arraySize; i++)
                {
                    // Get the Scene Beat
                    SerializedProperty beatProperty = beatsProperty.GetArrayElementAtIndex(i);
                    SceneBeatSO beat = beatProperty.objectReferenceValue as SceneBeatSO;

                    // Skip if the beat does not exist or the beat does not reference a branch
                    if (!beat || beat.Branches == null) continue;
                    
                    for (int j = 0; j < beat.Branches.Count; j++)
                    {
                        // Skip if the branch is not a ScriptableObject
                        if (beat.Branches[j] is not Object branchObj) continue;
                            
                        referencedBranches.Add(branchObj);
                    }
                }
            }
            
            // Find orphaned branches
            int orphanedCount = 0;
            StringBuilder logBuilder = new StringBuilder();

            for (int i = 0; i < allAssets.Length; i++)
            {
                Object asset = allAssets[i];
                
                // Skip if the asset is the current template
                if (!asset || asset == _currentTemplate) continue;
                
                // Skip if not a branch
                if (asset is not BeatBranchSO branch) continue;
                
                // Skip if the branch is referenced by a beat
                if (referencedBranches.Contains(asset)) continue;

                // Log the orphaned branch
                logBuilder.Append(" - ");
                logBuilder.Append(asset.name);
                logBuilder.Append("\n");
                
                // Destroy the asset
                AssetDatabase.RemoveObjectFromAsset(branch);
                DestroyImmediate(branch, true);
                orphanedCount++;
            }

            if (orphanedCount > 0)
            {
                // Save the changes
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.Append("[SceneTemplateEditor] Cleaned up ");
                messageBuilder.Append(orphanedCount);
                messageBuilder.Append(" orphaned branch(es):\n");
                messageBuilder.Append(logBuilder.ToString());
                Debug.Log(messageBuilder.ToString());

                EditorUtility.DisplayDialog("Cleanup Complete", messageBuilder.ToString(), "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Cleanup Complete", "No orphaned branches found", "OK");
            }

            // Refresh the graph view
            InitializeGraphView();
        }

        private void OnGraphViewMouseMove(MouseMoveEvent evt)
        {
            Vector2 mousePos = evt.localMousePosition;
            
            // Find the closest connection to the mouse
            BeatConnectionView closestConnection = null;
            float closestDistance = float.MaxValue;

            _graphView.Query<BeatConnectionView>().ForEach(connection =>
            {
                float distance = connection.GetDistanceToPoint(mousePos);

                // Exit case - if the distance is greater than the current closest
                if (distance >= 15f) return;
                if (distance >= closestDistance) return;
                
                closestConnection = connection;
                closestDistance = distance;
            });
            
            // Exit case - the connection didn't update
            if (_hoveredConnection == closestConnection) return;
            
            // Update the hover state
            _hoveredConnection?.SetHovered(false);
            _hoveredConnection = closestConnection;
            _hoveredConnection?.SetHovered(true);
        }


        /// <summary>
        /// 
        /// </summary>
        private void ValidateScene()
        {
            Debug.Log("$[SceneTemplateEditor] ValidatingScene() called");
            
            // Clear the section
            _validationSection.Clear();

            if (!_currentTemplate)
            {
                AddValidationMessage("No template selected", "error");
                return;
            }
            
            Debug.Log($"[SceneTemplateEditor] Validating scene template {_currentTemplate.name}");

            int errorCount = 0;
            int warningCount = 0;
            
            // Validate that the template has a starting beat
            if (string.IsNullOrEmpty(_currentTemplate.StartingBeatID))
            {
                AddValidationMessage("Scene template has no starting beat ID", "error");
                errorCount++;
            }
            
            // Get all beats
            SerializedObject serializedObject = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serializedObject.FindProperty("beats");

            // Exit case - no beats property found
            if (beatsProperty == null || beatsProperty.arraySize == 0)
            {
                AddValidationMessage("Scene template has no beats", "error");
                return;
            }
            
            StringBuilder logBuilder = new StringBuilder();
            
            // Validate each beat
            for (int i = 0; i < beatsProperty.arraySize; i++)
            {
                SerializedProperty beatProperty = beatsProperty.GetArrayElementAtIndex(i);
                SceneBeatSO beat = beatProperty.objectReferenceValue as SceneBeatSO;

                if (!beat)
                {
                    logBuilder.Clear();
                    logBuilder.Append("Beat at index '");
                    logBuilder.Append(i);
                    logBuilder.Append("' is null");
                    
                    AddValidationMessage(logBuilder.ToString(), "error");
                    errorCount++;
                    continue;
                }
                
                // Check beat ID
                if (string.IsNullOrEmpty(beat.BeatID))
                {
                    logBuilder.Clear();
                    logBuilder.Append("Beat '");
                    logBuilder.Append(beat.name);
                    logBuilder.Append(" has no beat ID");
                    
                    AddValidationMessage(logBuilder.ToString(), "error");
                    errorCount++;
                }
                
                // Check speaker role
                if (string.IsNullOrEmpty(beat.SpeakerRoleID))
                {
                    logBuilder.Clear();
                    logBuilder.Append("Beat '");
                    logBuilder.Append(beat.name);
                    logBuilder.Append(" has no speaker role");
                    
                    AddValidationMessage(logBuilder.ToString(), "warning");
                    warningCount++;
                }
                
                // Check variation set
                if (string.IsNullOrEmpty(beat.VariationSetID))
                {
                    logBuilder.Clear();
                    logBuilder.Append("Beat '");
                    logBuilder.Append(beat.name);
                    logBuilder.Append(" has no variation set");
                    
                    AddValidationMessage(logBuilder.ToString(), "warning");
                    warningCount++;
                }
                
                // Skip if there are no branches
                if (beat.Branches == null) continue;
                
                // Validate branches
                for (int j = 0; j < beat.Branches.Count; j++)
                {
                    IBeatBranch branch = beat.Branches[j];
                    if (branch == null)
                    {
                        logBuilder.Clear();
                        logBuilder.Append("Beat '");
                        logBuilder.Append(beat.name);
                        logBuilder.Append(" has no null branch at index ");
                        logBuilder.Append(j);
                            
                        AddValidationMessage(logBuilder.ToString(), "error");
                        errorCount++;
                        continue;
                    }

                    // Skip if the branch has a next beat ID
                    if (!string.IsNullOrEmpty(branch.NextBeatID)) continue;
                    
                    logBuilder.Clear();
                    logBuilder.Append("Beat '");
                    logBuilder.Append(beat.name);
                    logBuilder.Append(" has a branch with no target");
                    errorCount++;
                }
            }
            
            // Summary message
            if (errorCount == 0 && warningCount == 0)
            {
                AddValidationMessage("No issues found!", "success");
            }
            else
            {
                logBuilder.Clear();
                logBuilder.Append("Found ");
                logBuilder.Append(errorCount);
                logBuilder.Append(" error(s) and ");
                logBuilder.Append(warningCount);
                logBuilder.Append(" warning(s)");

                AddValidationMessage(logBuilder.ToString(), errorCount > 0 ? "error" : "warning");
            }
        }

        /// <summary>
        /// Adds a validation message to the validation section in the Scene Template Editor Window;
        /// the message visually indicates issues or errors encountered during validation of the scene template
        /// </summary>
        /// <param name="message">The text of the validation message to be displayed</param>
        /// <param name="type">The type of the validation message (e.g., "error" or "warning"), which is used to determine the styling</param>
        private void AddValidationMessage(string message, string type)
        {
            Debug.Log($"[SceneTemplateEditor] Adding validation message: [{type}] {message}");
            
            VisualElement item = new VisualElement();
            StringBuilder classBuilder = new StringBuilder();
            classBuilder.Append("validation-");
            classBuilder.Append(type);
            
            // Add classes
            item.AddToClassList("validation-item");
            item.AddToClassList(classBuilder.ToString());
            
            // Create the label
            Label label = new Label(message);
            label.AddToClassList("validation-message");
            
            // Add to the validation section
            item.Add(label);
            _validationSection.Add(item);
        }
        
        /// <summary>
        /// Retrieves the main graph view element used for rendering and manipulation
        /// of the node-based editor interface in the Scene Template Editor Window
        /// </summary>
        /// <returns>The primary <see cref="VisualElement"/> representing the graph view</returns>
        public VisualElement GetGraphView() => _graphView;
    }
}