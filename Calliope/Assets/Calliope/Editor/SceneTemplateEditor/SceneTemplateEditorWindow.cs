using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Editor.BatchAssetCreator;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using FieldConfig = Calliope.Editor.SceneTemplateEditor.AssetCreationDialog.FieldConfig;

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
        private TextField _searchField;
        private Button _clearSearchButton;
        private string _currentSearchText = "";
        private Button _createBeatButton;
        private Button _cleanupButton;
        private Button _batchCreatorButton;
        private VisualElement _inspectorContent;
        private BeatNodeView _selectedNode;
        
        private Button _templateSelectorButton;
        private Label _templateSelectorText;
        private Button _renameTemplateButton;
        private VisualElement _recentTemplatesContainer;
        private VisualElement _recentTemplateButtons;
        private const string RecentTemplatesPrefKey = "Calliope_RecentTemplates";
        private const int MaxRecentTemplates = 5;
        
        private VisualElement _validationSection;
        private Button _validationButton;
        
        private BeatConnectionView _hoveredConnection;
        private BeatBranchSO _pendingConditionBranch;
        private BeatNodeView _pendingConditionNodeView;
        
        private VisualElement _graphContent;
        private Button _zoomInButton;
        private Button _zoomOutButton;
        private Button _zoomResetButton;
        private Label _zoomLevelLabel;
        private float _zoomLevel = 1f;
        private Vector2 _panOffset = Vector2.zero;
        private bool _isPanning = false;
        private Vector2 _lastMousePosition;
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 2f;
        private const float ZoomStep = 0.1f;
        
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
            _templateSelectorButton = root.Q<Button>("TemplateSelectorButton");
            _templateSelectorText = root.Q<Label>("TemplateSelectorText");
            _renameTemplateButton = root.Q<Button>("RenameTemplateButton");
            _recentTemplatesContainer = root.Q<VisualElement>("RecentTemplatesContainer");
            _recentTemplateButtons = root.Q<VisualElement>("RecentTemplateButtons");
            _createBeatButton = root.Q<Button>("CreateBeatButton");
            _cleanupButton = root.Q<Button>("CleanupButton");
            _inspectorContent = root.Q<VisualElement>("InspectorContent");
            _validationSection = root.Q<VisualElement>("ValidationSection");
            _validationButton = root.Q<Button>("ValidateButton");
            _searchField = root.Q<TextField>("SearchField");
            _batchCreatorButton = root.Q<Button>("BatchCreatorButton");
            _clearSearchButton = root.Q<Button>("ClearSearchButton");
            _zoomInButton = root.Q<Button>("ZoomInButton");
            _zoomOutButton = root.Q<Button>("ZoomOutButton");
            _zoomResetButton = root.Q<Button>("ZoomResetButton");
            _zoomLevelLabel = root.Q<Label>("ZoomLevelLabel");
            
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

            // Set up the search field
            _searchField?.RegisterValueChangedCallback(OnSearchChanged);

            // Set up the clear search button
            if (_clearSearchButton != null)
            {
                _clearSearchButton.clicked += OnClearSearchClicked;
            }
            
            // Set up the batch creator button
            if (_batchCreatorButton != null)
            {
                _batchCreatorButton.clicked += OnBatchCreatorClicked;
            }
            
            // Set up the zoom in button
            if(_zoomInButton != null)
            {
                _zoomInButton.clicked += () => Zoom(ZoomStep, null);
            }

            // Set up the zoom out button
            if (_zoomOutButton != null)
            {
                _zoomOutButton.clicked += () => Zoom(-ZoomStep, null);
            }

            // Set up the reset zoom button
            if (_zoomResetButton != null)
            {
                _zoomResetButton.clicked += ResetView;
            }

            UpdateZoomLevelLabel();
            
            // Set up rename template button
            if(_renameTemplateButton != null) 
            {
                _renameTemplateButton.clicked += OnRenameTemplateClicked;
                _renameTemplateButton.SetEnabled(false);
            }

            // Set up the template selector
            if (_templateSelectorButton != null)
            {
                _templateSelectorButton.clicked += ShowTemplateSelector;
                UpdateTemplateSelectorDisplay();
            }
            
            // Update recent templates
            UpdateRecentTemplatesUI();
            
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

        /// <summary>
        /// Creates a new SceneBeatSO asset with the given identifier and display name;
        /// assigns it to the currently selected SceneTemplateSO asset, optionally saves its position in the graph view,
        /// and updates the template and associated assets accordingly
        /// </summary>
        /// <param name="beatID">The unique identifier for the new beat</param>
        /// <param name="displayName">The display name for the new beat; if null or empty, the beat ID is used as the name</param>
        /// <param name="nodePosition">The optional position of the new beat in the graph
        private void CreateBeat(string beatID, string displayName, Vector2? nodePosition)
        {
            // Exit case - no template selected
            if (!_currentTemplate) return;
            
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
            
            // Display validation warnings
            List<string> warnings = ValidateBeatReferences(beat);
            if (warnings.Count > 0)
            {
                VisualElement warningBox = new VisualElement();
                warningBox.style.backgroundColor = new Color(0.8f, 0.5f, 0f, 0.3f);
                warningBox.style.borderLeftWidth = 3;
                warningBox.style.borderLeftColor = new Color(0.8f, 0.5f, 0f);
                warningBox.style.paddingLeft = 8;
                warningBox.style.paddingRight = 8;
                warningBox.style.paddingTop = 4;
                warningBox.style.paddingBottom = 4;
                warningBox.style.marginBottom = 8;

                Label warningTitle = new Label("⚠ Missing References");
                warningTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                warningTitle.style.color = new Color(1f, 0.7f, 0.2f);
                warningBox.Add(warningTitle);

                for (int i = 0; i < warnings.Count; i++)
                {
                    Label warningLabel = new Label(warnings[i]);
                    warningLabel.style.fontSize = 11;
                    warningLabel.style.color = new Color(1f, 0.8f, 0.4f);
                    warningBox.Add(warningLabel);
                }

                _inspectorContent.Add(warningBox);
            }

            // Beat ID field
            TextField beatIDField = new TextField("Beat ID");
            beatIDField.value = beat.BeatID ?? "";
            beatIDField.style.marginBottom = 8;
            beatIDField.isDelayed = true;
            beatIDField.RegisterValueChangedCallback(evt => OnBeatPropertyChanged(beat, "beatID", evt.newValue));
            _inspectorContent.Add(beatIDField);
            
            // Speaker role field
            VisualElement speakerField = CreateRoleFieldWithEdit(
                label: "Speaker Role",
                prefsKey: "SpeakerRole",
                currentValue: beat.SpeakerRoleID,
                onValueChanged: (value) => OnBeatPropertyChanged(beat, "speakerRoleID", value),
                onCreateNew: (roleID) => 
                {
                    OnBeatPropertyChanged(beat, "speakerRoleID", roleID);
                    ShowBeatInspector(nodeView);
                },
                nodeView: nodeView
            );
            _inspectorContent.Add(speakerField);
            
            // Target role field
            VisualElement targetField = CreateRoleFieldWithEdit(
                label: "Target Role",
                prefsKey: "TargetRole",
                currentValue: beat.TargetRoleID,
                onValueChanged: (value) => OnBeatPropertyChanged(beat, "targetRoleID", value),
                onCreateNew: (roleID) => 
                {
                    OnBeatPropertyChanged(beat, "targetRoleID", roleID);
                    ShowBeatInspector(nodeView);
                },
                nodeView: nodeView
            );
            _inspectorContent.Add(targetField);
            
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
            
            bool isCurrentlyStarting = !string.IsNullOrEmpty(_currentTemplate.StartingBeatID) && _currentTemplate.StartingBeatID == beat.BeatID;
            if (!isCurrentlyStarting)
            {
                Button setStartingButton = new Button(() => SetAsStartingBeat(beat, nodeView));
                setStartingButton.text = "Set as Starting Beat";
                setStartingButton.style.marginTop = 12;
                setStartingButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.3f);
                _inspectorContent.Add(setStartingButton);
            }
            else
            {
                Label startingLabel = new Label("✓ This is the Starting Beat");
                startingLabel.style.marginTop = 12;
                startingLabel.style.color = new Color(0.5f, 0.8f, 0.5f);
                startingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _inspectorContent.Add(startingLabel);
            }
            
            // Spacer
            VisualElement spacer = new VisualElement();
            spacer.style.height = 24;
            _inspectorContent.Add(spacer);
            
            // Create the branch editor section
            CreateBranchEditorSection(beat, nodeView);
            
            // Spacer before delete
            VisualElement spacer2 = new VisualElement();
            spacer2.style.height = 24;
            _inspectorContent.Add(spacer2);
            
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
            List<FieldConfig> fields = new List<FieldConfig>
            {
                new FieldConfig(
                    key: "roleID",
                    label: "Role ID",
                    required: true,
                    autoGenerateFromName: true,
                    transformForID: fieldName => fieldName.ToLower().Replace(" ", "-")
                ),
                new FieldConfig(
                    key: "displayName",
                    label: "Display Name",
                    required: false,
                    autoGenerateFromName: true,
                    transformForID: fieldName => fieldName
                )
            };
            
            AssetCreationDialog.Show(
                assetTypeName: "Role",
                defaultFolder: AssetCreator.GetDefaultFolder<SceneRoleSO>(),
                fields: fields,
                onCreate: (fieldValues, savePath) =>
                {
                    SceneRoleSO newRole = CreateInstance<SceneRoleSO>();
                    SerializedObject serialized = new SerializedObject(newRole);

                    SerializedProperty roleIDProp =
                        serialized.FindProperty("roleID");
                    SerializedProperty displayNameProp =
                        serialized.FindProperty("displayName");

                    // Set properties
                    if (roleIDProp != null)
                        roleIDProp.stringValue = fieldValues["roleID"];
                    if (displayNameProp != null)
                        displayNameProp.stringValue = fieldValues["displayName"];

                    serialized.ApplyModifiedProperties();

                    // Save the asset
                    AssetDatabase.CreateAsset(newRole, savePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    onRoleCreated?.Invoke(fieldValues["roleID"]);
                }
            );
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
            List<FieldConfig> fields = new List<FieldConfig>
            {
                new FieldConfig(
                    key: "id",
                    label: "Variation Set ID",
                    required: true,
                    autoGenerateFromName: true,
                    transformForID: fieldName => fieldName.ToLower().Replace(" ", "-")
                ),
                new FieldConfig(
                    key: "displayName",
                    label: "Display Name",
                    required: false,
                    autoGenerateFromName: true,
                    transformForID: fieldName => fieldName
                ),
                new FieldConfig(
                    key: "description",
                    label: "description",
                    required: false
                )
            };
            
            AssetCreationDialog.Show(
                assetTypeName: "Variation Set",
                defaultFolder: AssetCreator.GetDefaultFolder<VariationSetSO>(),
                fields: fields,
                onCreate: (fieldValues, savePath) =>
                {
                    VariationSetSO newVariationSet = CreateInstance<VariationSetSO>();
                    
                    SerializedObject serialized = new SerializedObject(newVariationSet);
                    SerializedProperty idProperty = serialized.FindProperty("id");
                    SerializedProperty displayNameProperty = serialized.FindProperty("displayName");
                    SerializedProperty descriptionProperty = serialized.FindProperty("description");

                    // Set the properties
                    if (idProperty != null) 
                        idProperty.stringValue = fieldValues["id"];
                    if (displayNameProperty != null) 
                        displayNameProperty.stringValue = fieldValues["displayName"];
                    if (descriptionProperty != null) 
                        descriptionProperty.stringValue = fieldValues.GetValueOrDefault("description", "");
                    
                    serialized.ApplyModifiedProperties();
                    
                    // Save the asset
                    AssetDatabase.CreateAsset(newVariationSet, savePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // Make it addressable
                    AssetCreator.MakeAssetAddressable<VariationSetSO>(savePath);
                    
                    onVariationSetCreated?.Invoke(fieldValues["id"]);
                }
            );
        }

        /// <summary>
        /// Sets the specified SceneBeatSO instance as the starting beat for the currently loaded SceneTemplateSO;
        /// updates the starting beat property, saves the asset changes, and refreshes the editor's visual state
        /// </summary>
        /// <param name="beat">The SceneBeatSO instance to be set as the starting beat</param>
        /// <param name="nodeView">The BeatNodeView instance representing the visual node associated with the beat</param>
        private void SetAsStartingBeat(SceneBeatSO beat, BeatNodeView nodeView)
        {
            // Exit case - no template or beat
            if (!_currentTemplate || !beat) return;

            SerializedObject templateSerialized = new SerializedObject(_currentTemplate);
            SerializedProperty startingBeatProperty = templateSerialized.FindProperty("startingBeatID");

            // Set the property value
            if (startingBeatProperty != null)
            {
                startingBeatProperty.stringValue = beat.BeatID;
                templateSerialized.ApplyModifiedProperties();
            }
            
            // Save the asset
            EditorUtility.SetDirty(_currentTemplate);
            AssetDatabase.SaveAssets();
            
            // Refresh the view
            InitializeGraphView();
            ShowBeatInspector(nodeView);
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
            SerializedObject serializedObject = new SerializedObject(beat);
            
            // Handle VariationSetID specially - it maps to a VariationSetSO object reference
            if (propertyName == "VariationSetID")
            {
                SerializedProperty variationSetProperty = serializedObject.FindProperty("variationSet");

                // Exit case - no variation set property found
                if (variationSetProperty == null) return;
                
                // Find the VariationSetSO asset by ID
                VariationSetSO variationSetAsset = null;
                if (!string.IsNullOrEmpty(newValue))
                {
                    List<VariationSetSO> variationSets = AssetCreator.FindAllAssets<VariationSetSO>();
                    for (int i = 0; i < variationSets.Count; i++)
                    {
                        // Skip if the variation set does not exist or its ID does not match the specified value
                        if (!variationSets[i] || variationSets[i].ID != newValue) continue;
                        
                        variationSetAsset = variationSets[i];
                        break;
                    }
                }
                
                // Set the property value
                variationSetProperty.objectReferenceValue = variationSetAsset;
                serializedObject.ApplyModifiedProperties();

                // Save the asset
                EditorUtility.SetDirty(beat);
                AssetDatabase.SaveAssets();
                InitializeGraphView();
            }
            
            // Get the property value
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

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

            // Save the old beat ID for migration purposes
            string oldBeatID = null;
            if (propertyName == "beatID")
            {
                oldBeatID = property.stringValue;
            }
            
            // Set the property value
            property.stringValue = newValue;

            // Automatically add roles to the template
            if (propertyName == "speakerRoleID" || propertyName == "targetRoleID")
                AddRoleToTemplateIfMissing(newValue);
            
            // Apply the changes
            serializedObject.ApplyModifiedProperties();
            
            // Migrate node position if beat ID changed
            if (propertyName == "beatID" && _currentTemplate != null && !string.IsNullOrEmpty(oldBeatID))
            {
                // Load the current position of the beat
                Vector2 currentPosition = NodePositionStorage.LoadPosition(_currentTemplate.ID, oldBeatID, Vector2.zero);

                // Save the new key and delete the old key
                if (currentPosition != Vector2.zero)
                {
                    NodePositionStorage.SavePosition(_currentTemplate.ID, newValue, currentPosition);
                    NodePositionStorage.DeletePosition(_currentTemplate.ID, oldBeatID);
                }
            }

            // Mark the editor as dirty and save assets
            EditorUtility.SetDirty(beat);
            AssetDatabase.SaveAssets();

            // Refresh the node view
            InitializeGraphView();

            // Exit case - no selected node or beat
            if (_selectedNode == null || _selectedNode.Beat != beat) return;
            
            // Refresh the beat inspector
            ShowBeatInspector(_selectedNode);
        }

        /// <summary>
        /// Ensures that a role specified by its unique identifier is included in the current scene template;
        /// if the role is not already present, it searches for the corresponding SceneRoleSO asset,
        /// adds it to the template, and persists the changes to the asset database
        /// </summary>
        /// <param name="roleID">The unique identifier of the role to be added to the scene template</param>
        private void AddRoleToTemplateIfMissing(string roleID)
        {
            // Exit case - no template or empty role ID
            if (!_currentTemplate || string.IsNullOrEmpty(roleID)) return;
            
            // Check if the role is already in the template
            IReadOnlyList<ISceneRole> existingRoles = _currentTemplate.Roles;
            if (existingRoles != null)
            {
                for (int i = 0; i < existingRoles.Count; i++)
                {
                    // Skip if the role does not exist or its ID does not match the specified value
                    if (existingRoles[i] == null || existingRoles[i].RoleID != roleID)
                        continue;

                    // Exit case - the role already exists in the template
                    return;
                }
            }
            
            // Find the SceneRoleSO asset by ID
            SceneRoleSO roleAsset = null;
            List<SceneRoleSO> allRoles = AssetCreator.FindAllAssets<SceneRoleSO>();
            for (int i = 0; i < allRoles.Count; i++)
            {
                // Skip if the role does not exist or its ID does not match the specified value
                if (!allRoles[i] || allRoles[i].RoleID != roleID) continue;
                
                roleAsset = allRoles[i];
                break;
            }
            
            // Exit case - no role asset found
            if (!roleAsset) return;
            
            // Add the role to the template's roles array
            SerializedObject templateSerialized = new SerializedObject(_currentTemplate);
            SerializedProperty rolesProperty = templateSerialized.FindProperty("roles");

            // Exit case - the roles property does not exist on the template
            if (rolesProperty == null) return;
            
            // Expand array and add the new role
            int newIndex = rolesProperty.arraySize;
            rolesProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElement = rolesProperty.GetArrayElementAtIndex(newIndex);
            newElement.objectReferenceValue = roleAsset;
            
            // Save the asset
            templateSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_currentTemplate);
            AssetDatabase.SaveAssets();
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
            
            // Cache the zoom controls before clearing (defined in the UXML)
            VisualElement zoomControls = _graphView.Q<VisualElement>("ZoomControlsContainer");
            
            // Clear any existing content
            _graphView.Clear();
            
            // Create content for zoom/pan
            _graphContent = new VisualElement();
            _graphContent.name = "GraphContent";
            _graphContent.AddToClassList("graph-content");
            _graphContent.pickingMode = PickingMode.Ignore;
            _graphView.Add(_graphContent);
            
            // Re-add zoom controls
            if(zoomControls != null) _graphView.Add(zoomControls);
            
            // Apply the current zoom and pan
            ApplyZoomAndPan();
            
            // Register mouse handlers
            _graphView.RegisterCallback<MouseMoveEvent>(OnGraphViewMouseMove);
            _graphView.RegisterCallback<MouseDownEvent>(OnGraphViewMouseDown, TrickleDown.TrickleDown);
            _graphView.RegisterCallback<WheelEvent>(OnMouseWheel);
            _graphView.RegisterCallback<MouseDownEvent>(OnPanStart);
            _graphView.RegisterCallback<MouseMoveEvent>(OnPanMove);
            _graphView.RegisterCallback<MouseUpEvent>(OnPanEnd);
            
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
                
                _graphContent.Add(noBeatLabel);
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
                
                // Validate the beat node
                List<string> warnings = ValidateBeatReferences(beat);
                if(warnings.Count > 0) 
                {
                    string tooltipText = string.Join("\n", warnings);
                    beatNodeView.SetWarningState(warnings.Count, tooltipText);
                }
                
                // Add the click handler for selection
                beatNodeView.RegisterCallback<MouseDownEvent>(evt => OnBeatNodeClicked(beatNodeView, evt));
                
                // Add connection drag manipulator to the output port
                beatNodeView.OutputPort?.AddManipulator(new ConnectionDragManipulator(beatNodeView, this));

                // Mark as starting beat, if applicable
                if (!string.IsNullOrEmpty(_currentTemplate.StartingBeatID) && beat.BeatID == _currentTemplate.StartingBeatID)
                {
                    beatNodeView.SetStartingBeat(true);
                }
                
                _graphContent.Add(beatNodeView);
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
            List<BeatNodeView> beatNodes = _graphContent.Query<BeatNodeView>().ToList();
            
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
                        _graphContent.Insert(0, connection);
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
            _graphContent.Query<BeatNodeView>().ForEach(node => node.SetSelected(false));
            
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
            _graphContent.Query<BeatConnectionView>().ForEach(connection => connection.UpdateConnection());
        }

        /// <summary>
        /// Displays a context menu for selecting or creating a SceneTemplateSO asset;
        /// provides an interface for users to choose existing SceneTemplateSO assets grouped by folder,
        /// or create a new template if none are available
        /// </summary>
        private void ShowTemplateSelector()
        {
            GenericMenu menu = new GenericMenu();
            
            // Find all SceneTemplateSO assets
            string[] guids = AssetDatabase.FindAssets("t:SceneTemplateSO");

            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No templates found"));
            }
            else
            {
                // Group templates by folder
                Dictionary<string, List<(string path, SceneTemplateSO template)>> groupedTemplates = new Dictionary<string, List<(string, SceneTemplateSO)>>();

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    SceneTemplateSO template = AssetDatabase.LoadAssetAtPath<SceneTemplateSO>(path);

                    // Skip if the template is null
                    if (template == null) continue;

                    // Get the folder path for grouping
                    string folderPath = Path.GetDirectoryName(path); folderPath = folderPath?.Replace("Assets/", "") ?? "Root";

                    // If the folder path is not already in the dictionary, add it
                    if (!groupedTemplates.ContainsKey(folderPath))
                        groupedTemplates[folderPath] = new List<(string, SceneTemplateSO)>();

                    groupedTemplates[folderPath].Add((path, template));
                }

                StringBuilder menuBuilder = new StringBuilder();

                // Add templates to the menu, grouped by folder
                foreach (KeyValuePair<string, List<(string path, SceneTemplateSO template)>> group in groupedTemplates)
                {
                    foreach ((string path, SceneTemplateSO template) in group.Value)
                    {
                        // Check if the template is already selected
                        bool isSelected = _currentTemplate == template;
                        
                        // Create the path
                        menuBuilder.Clear();
                        menuBuilder.Append(group.Key);
                        menuBuilder.Append("/");
                        menuBuilder.Append(template.name);
                        string menuPath = menuBuilder.ToString();

                        // Add the template to the menu
                        SceneTemplateSO templateRef = template;
                        menu.AddItem(
                            new GUIContent(menuPath),
                            isSelected,
                            () => SetCurrentTemplate(templateRef)
                        );
                    }
                }
            }

            menu.AddSeparator("");

            // Add the "Create New Template" option
            menu.AddItem(new GUIContent("+ Create New Template..."), false, ShowCreateTemplateDialog);

            menu.ShowAsContext();
        }

        /// <summary>
        /// Updates the display of the template selector UI to reflect the currently selected SceneTemplateSO;
        /// adjusts the label text and applies appropriate styling based on whether a template is selected or not
        /// </summary>
        private void UpdateTemplateSelectorDisplay()
        {
            // Exit case - no template is selected
            if (_templateSelectorButton == null || _templateSelectorText == null) 
                return;

            // Check if there's a current template
            if (_currentTemplate != null)
            {
                // If so, update the display text to reflect the template's name'
                _templateSelectorText.text = _currentTemplate.name;
                _templateSelectorButton.RemoveFromClassList("template-selector-no-value");
                _templateSelectorButton.AddToClassList("template-selector-has-value");
            }
            else
            {
                // Otherwise, show the placeholder text
                _templateSelectorText.text = "Select Template...";
                _templateSelectorButton.RemoveFromClassList("template-selector-has-value");
                _templateSelectorButton.AddToClassList("template-selector-no-value");
            }
            
            // Show the rename template button
            _renameTemplateButton?.SetEnabled(_currentTemplate != null);
        }

        /// <summary>
        /// Sets the specified SceneTemplateSO as the current template in the editor window;
        /// updates the template selector display and initializes the graph view with the nodes and connections of the selected template
        /// </summary>
        /// <param name="template">The SceneTemplateSO to set as the current template</param>
        private void SetCurrentTemplate(SceneTemplateSO template)
        {
            _currentTemplate = template;
            
            // Track in recent templates
            if (template != null) AddToRecentTemplates(template);
            
            UpdateTemplateSelectorDisplay();
            InitializeGraphView();
            ShowBeatInspector(null);
        }

        /// <summary>
        /// Adds the provided SceneTemplateSO asset to the list of recently used scene templates,
        /// ensuring it appears at the front of the list and maintaining a maximum limit of stored templates;
        /// updates the stored list in EditorPrefs for persistent access across editor sessions
        /// </summary>
        /// <param name="template">The SceneTemplateSO asset to add to the list of recent templates</param>
        private void AddToRecentTemplates(SceneTemplateSO template)
        {
            string templatePath = AssetDatabase.GetAssetPath(template);
            
            // Exit case - the template path is empty
            if (string.IsNullOrEmpty(templatePath)) return;
            
            // Get current recent list
            List<string> recentPaths = GetRecentTemplatePaths();
            
            // Remove if already exists (we'll re-add at the front)
            recentPaths.Remove(templatePath);
            
            // Add to the front
            recentPaths.Insert(0, templatePath);
            
            // Trim to max size
            while (recentPaths.Count > MaxRecentTemplates)
            {
                recentPaths.RemoveAt(recentPaths.Count - 1);
            }
            
            // Save
            string joined = string.Join("|", recentPaths);
            EditorPrefs.SetString(RecentTemplatesPrefKey, joined);
        }

        /// <summary>
        /// Retrieves a list of recent SceneTemplateSO asset paths stored in the editor preferences
        /// the paths are stored as a single string, with individual paths separated by a pipe ('|') character;
        /// returns an empty list if no recent paths are found or if the data is empty
        /// </summary>
        /// <returns>A list of strings representing the file paths of recently accessed SceneTemplateSO assets</returns>
        private List<string> GetRecentTemplatePaths()
        {
            string saved = EditorPrefs.GetString(RecentTemplatesPrefKey, "");

            // Exit case - no saved templates
            if (string.IsNullOrEmpty(saved)) return new List<string>();
            
            return new List<string>(saved.Split('|'));
        }

        /// <summary>
        /// Updates the Recent Templates section of the Scene Template Editor window.
        /// Clears the existing list of recent template buttons, retrieves the latest
        /// recent templates, and updates the UI accordingly; hides the container if no
        /// recent templates are found and generates buttons for each available recent
        /// template, linking them to respective templates for selection
        /// </summary>
        private void UpdateRecentTemplatesUI()
        {
            // Exit case - no template selector button
            if (_recentTemplateButtons == null) return;
            
            // Clear the recent templates
            _recentTemplateButtons.Clear();

            // Retrieve the recent templates
            List<string> recentPaths = GetRecentTemplatePaths();
            
            // Hide the container if no recent templates
            if (_recentTemplatesContainer != null)
            {
                _recentTemplatesContainer.style.display =
                    recentPaths.Count > 0 
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            // Add the recent templates to the UI
            for (int i = 0; i < recentPaths.Count; i++)
            {
                // Load the template
                string path = recentPaths[i];
                SceneTemplateSO template = AssetDatabase.LoadAssetAtPath<SceneTemplateSO>(path);
                
                // Skip if the template doesn't exist
                if(!template) continue;
                
                Button recentButton = new Button(() => SetCurrentTemplate(template));
                recentButton.text = template.name;
                recentButton.tooltip = path;
                
                recentButton.AddToClassList("recent-template-button");
                
                // Highlight if this is the current template
                if(_currentTemplate == template)
                {
                    recentButton.AddToClassList("recent-template-button-active");
                }
                
                _recentTemplateButtons.Add(recentButton);
            }
        }

        /// <summary>
        /// Displays a dialog to create a new SceneTemplateSO asset;
        /// allows the user to specify custom field values such as Template ID, Display Name, and Description,
        /// auto-generates fields where applicable, and ensures proper folder structure before saving the asset;
        /// the newly created template is saved to the specified location and selected in the editor
        /// </summary>
        private void ShowCreateTemplateDialog()
        {
            // Get default folder
            string defaultFolder = "Assets/Calliope/Content/SceneTemplates";

            List<FieldConfig> fields = new List<FieldConfig>
            {
                new FieldConfig(
                    key: "id",
                    label: "Template ID",
                    required: true,
                    autoGenerateFromName: false,
                    transformForID: fieldName => fieldName.ToLower().Replace(" ", "-")
                ),
                new FieldConfig(
                    key: "displayName",
                    label: "Display Name",
                    required: false,
                    autoGenerateFromName: false,
                    transformForID: fieldName => fieldName
                ),
                new FieldConfig(
                    key: "description",
                    label: "Description",
                    required: false
                )
            };

            AssetCreationDialog.Show(
                assetTypeName: "Scene Template",
                defaultFolder: defaultFolder,
                fields: fields,
                onCreate: (fieldValues, savePath) =>
                {
                    // Create the new template
                    SceneTemplateSO newTemplate = CreateInstance<SceneTemplateSO>();

                    SerializedObject serialized = new SerializedObject(newTemplate);

                    SerializedProperty idProp = serialized.FindProperty("id");
                    SerializedProperty displayNameProp = serialized.FindProperty("displayName");
                    SerializedProperty descriptionProp = serialized.FindProperty("description");

                    // Set the property values
                    if (idProp != null)
                        idProp.stringValue = fieldValues["id"];
                    if (displayNameProp != null)
                        displayNameProp.stringValue = fieldValues["displayName"];
                    if (descriptionProp != null)
                        descriptionProp.stringValue = fieldValues.GetValueOrDefault("description", "");

                    serialized.ApplyModifiedProperties();

                    // Ensure folder exists
                    string folderPath = Path.GetDirectoryName(savePath);
                    
                    if (!string.IsNullOrEmpty(folderPath) && !AssetDatabase.IsValidFolder(folderPath))
                    {
                        // Parse the folder path into components
                        string[] parts = folderPath.Split('/');
                        string currentPath = parts[0];

                        StringBuilder pathBuilder = new StringBuilder();
                        
                        // Create the folder hierarchy
                        for (int i = 1; i < parts.Length; i++)
                        {
                            // Construct the next path
                            pathBuilder.Clear();
                            pathBuilder.Append(currentPath);
                            pathBuilder.Append("/");
                            pathBuilder.Append(parts[i]);
                            string nextPath = pathBuilder.ToString();
                            
                            if (!AssetDatabase.IsValidFolder(nextPath))
                            {
                                AssetDatabase.CreateFolder(currentPath, parts[i]);
                            }
                            currentPath = nextPath;
                        }
                    }

                    // Save the asset
                    AssetDatabase.CreateAsset(newTemplate, savePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // Select the new template
                    SetCurrentTemplate(newTemplate);
                }
            );
        }

        /// <summary>
        /// Saves the current positions of all beat nodes in the graph view;
        /// iterates through the visible BeatNodeView elements, retrieves their positions,
        /// and stores the data in the NodePositionStorage using the template's and beat's unique identifiers;
        /// this ensures that the node layout is preserved between sessions
        /// </summary>
        public void SaveNodePositions()
        {
            // Exit case - no template is selected
            if (!_currentTemplate) return;
            
            // Save the position for each node
            _graphContent.Query<BeatNodeView>().ForEach(nodeView =>
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

        /// <summary>
        /// Handles the mouse move event within the graph view; identifies the connection closest to the mouse position,
        /// updates the hover state for the relevant connection, and ensures only one connection is marked as hovered at a time
        /// </summary>
        /// <param name="evt">The MouseMoveEvent triggered by the user's cursor movement within the graph view</param>
        private void OnGraphViewMouseMove(MouseMoveEvent evt)
        {
            Vector2 mousePos = evt.localMousePosition;

            Vector2 transformedPos = (mousePos - _panOffset) / _zoomLevel;
            
            // Find the closest connection to the mouse
            BeatConnectionView closestConnection = null;
            float closestDistance = float.MaxValue;

            _graphContent.Query<BeatConnectionView>().ForEach(connection =>
            {
                float distance = connection.GetDistanceToPoint(transformedPos);

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
        /// Handles the mouse down event on the graph view;
        /// displays a context menu for deleting a connection if the right mouse button is clicked
        /// while a connection is hovered over
        /// </summary>
        /// <param name="evt">The MouseDownEvent containing information about the mouse click</param>
        private void OnGraphViewMouseDown(MouseDownEvent evt)
        {
            // Exit case - not the right mouse button
            if (evt.button != 1) return;

            Vector2 mousePos = evt.mousePosition;
            Vector2 localMousePosition = _graphView.WorldToLocal(mousePos);
            
            // Show the Connection Context Menu if hovering a connection
            if (_hoveredConnection != null)
            {
                ShowConnectionContextMenu(_hoveredConnection);
                evt.StopPropagation();
                return;
            }
            
            // Show the Beat Context Menu if hovering or selecting a node
            BeatNodeView clickedNode = FindNodeAtPosition(mousePos);
            if (clickedNode != null)
            {
                ShowBeatContextMenu(clickedNode, localMousePosition);
                evt.StopPropagation();
                return;
            }
            
            // Show the Empty Context menu if hovering over an empty space
            ShowEmptySpaceContextMenu(localMousePosition);
            
            evt.StopPropagation();
        }

        /// <summary>
        /// Finds and returns the <see cref="BeatNodeView"/> located at the specified screen position;
        /// traverses the hierarchy of UI elements in the graph view to locate the node at the given screen coordinates
        /// </summary>
        /// <param name="screenPosition">The position on the screen in world coordinates where the search for a node begins</param>
        /// <returns>Returns the <see cref="BeatNodeView"/> instance at the specified position if found; otherwise, returns null</returns>
        private BeatNodeView FindNodeAtPosition(Vector2 screenPosition)
        {
            VisualElement targetElement = _graphView.panel.Pick(screenPosition);
            
            // Walk up the hierarchy to find a BeatNodeView
            VisualElement current = targetElement;
            while (current != null)
            {
                if(current is BeatNodeView nodeView) return nodeView;
                current = current.parent;
            }
            
            return null;
        }

        /// <summary>
        /// Displays a context menu for a specified connection between two beat nodes in the editor;
        /// allows users to view information about the connection or delete it from the graph
        /// </summary>
        /// <param name="connection">The connection view representing the link between two beat nodes</param>
        private void ShowConnectionContextMenu(BeatConnectionView connection)
        {
            GenericMenu menu = new GenericMenu();
            
            // Header (disabled item to show what's selected)
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("Branch: ");
            headerBuilder.Append(connection.FromNode?.Beat?.BeatID ?? "?");
            headerBuilder.Append(" to ");
            headerBuilder.Append(connection.ToNode?.Beat?.BeatID ?? "?");
            menu.AddDisabledItem(new GUIContent(headerBuilder.ToString()));
            menu.AddSeparator("");
            
            // Delete
            menu.AddItem(new GUIContent("Delete Connection"), false, () =>
            {
                DeleteBranch(connection.FromNode, connection.ToNode, connection.Branch);
            });
            
            menu.ShowAsContext();;
        }

        /// <summary>
        /// Displays a context menu for the selected BeatNodeView, providing options to interact with the beat,
        /// such as selecting it, setting it as the starting beat, creating branches, or deleting it
        /// </summary>
        /// <param name="node">The BeatNodeView instance for which the context menu is being displayed</param>
        /// <param name="localMousePosition">The local position of the mouse pointer when the context menu is triggered</param>
        private void ShowBeatContextMenu(BeatNodeView node, Vector2 localMousePosition)
        {
            GenericMenu menu = new GenericMenu();
            
            // Header
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("Beat: ");
            headerBuilder.Append(node.Beat?.BeatID ?? node.Beat?.name ?? "Unknown");
            menu.AddDisabledItem(new GUIContent(headerBuilder.ToString()));
            menu.AddSeparator("");;
            
            // Set as the Starting Beat option
            bool isStartingBeat = !string.IsNullOrEmpty(_currentTemplate?.StartingBeatID) && _currentTemplate.StartingBeatID == node.Beat?.BeatID;
            if (!isStartingBeat)
            {
                menu.AddItem(new GUIContent("Set as Starting Beat"), false, () =>
                {
                    SetAsStartingBeat(node.Beat, node);
                });
            }
            else {
                menu.AddDisabledItem(new GUIContent("Set as Starting Beat (Already Starting)"));
            }
            
            menu.AddSeparator("");
            
            StringBuilder menuPath = new StringBuilder();
            
            // Create Branch submenu (list all possible target beats)
            List<SearchableDropdown.DropdownItem> targetBeats = GetBeatDropdownItems(node.Beat);
            if(targetBeats.Count > 0) 
            {
                for(int i = 0; i < targetBeats.Count; i++) 
                {
                    SearchableDropdown.DropdownItem targetBeat = targetBeats[i];
                    
                    // Check if the branch already exists
                    bool branchExists = false;
                    if(node.Beat?.Branches != null) 
                    {
                        for (int j = 0; j < node.Beat.Branches.Count; j++)
                        {
                            // Skip if the branches' target beat ID does not match the target beat
                            if(node.Beat.Branches[j]?.NextBeatID != targetBeat.ID) continue;
                            
                            branchExists = true;
                            break;
                        }
                    }

                    menuPath.Clear();
                    menuPath.Append("Create Branch To \"");
                    menuPath.Append(targetBeat.DisplayName);
                    
                    if (branchExists)
                    {
                        // Disable the item if the branch already exists
                        menuPath.Append("\" (exists");
                        menu.AddDisabledItem(new GUIContent(menuPath.ToString()));
                    } 
                    else
                    {
                        menuPath.Append("\"");
                        // Create a menu item to branch
                        string targetID = targetBeat.ID;
                        menu.AddItem(new GUIContent(menuPath.ToString()), false, () =>
                        {
                            CreateNewBranch(node.Beat, targetID, node);
                        });
                    }
                }
            } 
            else 
            {
                menu.AddDisabledItem(new GUIContent("Create Branch To (No other beats available)"));
            }
            
            menu.AddSeparator("");
                
            // Delete option
            menu.AddItem(new GUIContent("Delete Beat"), false, () => 
            {
                OnDeleteButtonClicked(node);;
            });
                
            menu.ShowAsContext();
        }

        /// <summary>
        /// Displays a context menu at the specified position when the user interacts with empty space in the editor;
        /// this menu provides options for creating new beats, validating the scene, and cleaning up orphaned branches
        /// </summary>
        /// <param name="localMousePosition">The position in local space where the context menu should be shown</param>
        private void ShowEmptySpaceContextMenu(Vector2 localMousePosition)
        {
            // Exit case - no template has been selected
            if (!_currentTemplate)
            {
                GenericMenu noTemplateMenu = new GenericMenu();
                noTemplateMenu.AddDisabledItem(new GUIContent("Select a template first"));
                noTemplateMenu.ShowAsContext();
                return;
            }
            
            GenericMenu menu = new GenericMenu();
            
            // Create a beat at this position
            menu.AddItem(new GUIContent("Create New Beat"), false, () =>
            {
                BeatCreationDialog.Show(
                    onCreate: (beatID, displayName) => CreateBeat(beatID, displayName, localMousePosition),
                    onValidateID: IsBeatIDUnique
                );
            });
            
            menu.AddSeparator("");
            
            // Quick actions
            menu.AddItem(new GUIContent("Validate Scene"), false, ValidateScene);
            menu.AddItem(new GUIContent("Cleanup Orphaned Branches"), false, CleanupOrphanedBranches);
            
            menu.ShowAsContext();
        }

        /// <summary>
        /// Validates the currently selected SceneTemplateSO instance by checking for critical issues such as missing beats,
        /// invalid properties, and improperly configured branches; logs errors and warnings, updates the validation section
        /// of the UI with detailed messages, and provides an overall summary of the validation results
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

        /// <summary>
        /// Creates and configures the branch editor section within the scene template editor window;
        /// adds a UI section for managing branches of the provided SceneBeatSO, allowing for the addition, display,
        /// and editing of beat branches while linking them to the specified BeatNodeView;
        /// also incorporates a section for setting the default next beat
        /// </summary>
        /// <param name="beat">The SceneBeatSO instance containing the beat data, including its branches, to be displayed and managed</param>
        /// <param name="nodeView">The BeatNodeView instance representing the visual context for the beat within the editor hierarchy</param>
        private void CreateBranchEditorSection(SceneBeatSO beat, BeatNodeView nodeView)
        {
            // Section header
            VisualElement branchHeader = new VisualElement();
            branchHeader.style.flexDirection = FlexDirection.Row;
            branchHeader.style.justifyContent = Justify.SpaceBetween;
            branchHeader.style.alignItems = Align.Center;
            branchHeader.style.marginTop = 16;
            branchHeader.style.marginBottom = 8;

            Label branchLabel = new Label("Branches");
            branchLabel.style.fontSize = 14;
            branchLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            branchHeader.Add(branchLabel);

            Button addBranchButton = new Button(() => OnAddBranchClicked(beat, nodeView));
            addBranchButton.text = "+ Add Branch";
            addBranchButton.style.paddingLeft = 8;
            addBranchButton.style.paddingRight = 8;
            branchHeader.Add(addBranchButton);

            _inspectorContent.Add(branchHeader);

            // Branch list container
            VisualElement branchList = new VisualElement();
            branchList.AddToClassList("branch-list");
            branchList.style.borderTopWidth = 1;
            branchList.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            branchList.style.paddingTop = 8;
            
            // Get branches
            SerializedObject beatSerializedObject = new SerializedObject(beat);
            SerializedProperty branchesProperty = beatSerializedObject.FindProperty("branches");

            // Check if there are branches
            if (branchesProperty == null || branchesProperty.arraySize == 0)
            {
                // If not, add a message
                Label noBranchesLabel = new Label("No branches. This beat will end the scene.");
                noBranchesLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                noBranchesLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                noBranchesLabel.style.marginTop = 4;
                branchList.Add(noBranchesLabel);
            }
            else
            {
                // Add each branch to the list
                for (int i = 0; i < branchesProperty.arraySize; i++)
                {
                    int branchIndex = i;
                    SerializedProperty branchProperty = branchesProperty.GetArrayElementAtIndex(i);
                    BeatBranchSO branch = branchProperty.objectReferenceValue as BeatBranchSO;

                    // Skip if the branch is null
                    if (!branch) continue;

                    // Create the visual representation of the branch
                    VisualElement branchItem = CreateBranchItemUI(
                        beat,
                        branch,
                        branchIndex,
                        branchesProperty.arraySize,
                        nodeView
                    );
                    
                    branchList.Add(branchItem);
                }
            }
            
            _inspectorContent.Add(branchList);
                
            // Default next beat section
            CreateDefaultNextBeatSection(beat, nodeView);
        }

        /// <summary>
        /// Creates a UI element representing a branch item within a scene template editor window;
        /// this method initializes and configures the branch UI using the provided scene beat, branch data,
        /// and positional context, ensuring proper integration with its corresponding beat node view
        /// </summary>
        /// <param name="beat">The SceneBeatSO object associated with the branch item</param>
        /// <param name="branch">The BeatBranchSO object containing the data for the branch</param>
        /// <param name="index">The positional index of the branch within the list of branches</param>
        /// <param name="totalBranches">The total number of branches in the context of the scene beat</param>
        /// <param name="nodeView">The BeatNodeView object to which the branch item is related</param>
        /// <returns>Returns a VisualElement that represents the created branch item in the editor UI</returns>
        private VisualElement CreateBranchItemUI(
            SceneBeatSO beat,
            BeatBranchSO branch,
            int index,
            int totalBranches,
            BeatNodeView nodeView
        )
        {
            VisualElement branchItem = new VisualElement();
            branchItem.AddToClassList("branch-item");
            branchItem.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            branchItem.style.borderTopLeftRadius = 4;
            branchItem.style.borderTopRightRadius = 4;
            branchItem.style.borderBottomLeftRadius = 4;
            branchItem.style.borderBottomRightRadius = 4;
            branchItem.style.marginBottom = 8;
            branchItem.style.paddingTop = 8;
            branchItem.style.paddingBottom = 8;
            branchItem.style.paddingLeft = 8;
            branchItem.style.paddingRight = 8;

            // Header row with priority number and controls
            VisualElement headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 8;

            // Priority label
            StringBuilder priorityBuilder = new StringBuilder();
            priorityBuilder.Append("Priority ");
            priorityBuilder.Append(index + 1);
            Label priorityLabel = new Label(priorityBuilder.ToString());
            priorityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            priorityLabel.style.color = new Color(0.8f, 0.8f, 0.5f);
            headerRow.Add(priorityLabel);

            // Control buttons container
            VisualElement controlButtons = new VisualElement();
            controlButtons.style.flexDirection = FlexDirection.Row;

            // Move up button
            Button moveUpButton = new Button(() => MoveBranch(beat, index, -1, nodeView));
            moveUpButton.text = "▲";
            moveUpButton.style.width = 24;
            moveUpButton.style.marginRight = 2;
            moveUpButton.SetEnabled(index > 0);
            controlButtons.Add(moveUpButton);

            // Move down button
            Button moveDownButton = new Button(() => MoveBranch(beat, index, 1, nodeView));
            moveDownButton.text = "▼";
            moveDownButton.style.width = 24;
            moveDownButton.style.marginRight = 8;
            moveDownButton.SetEnabled(index < totalBranches - 1);
            controlButtons.Add(moveDownButton);

            // Delete button
            Button deleteBranchButton = new Button(() => DeleteBranchAtIndex(beat, index, nodeView));
            deleteBranchButton.text = "X";
            deleteBranchButton.style.width = 24;
            deleteBranchButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            controlButtons.Add(deleteBranchButton);

            headerRow.Add(controlButtons);
            branchItem.Add(headerRow);

            // Target beat dropdown
            VisualElement targetRow = new VisualElement();
            targetRow.style.marginBottom = 8;

            SearchableDropdown targetDropdown = new SearchableDropdown(
                label: "Target Beat",
                prefsKey: "BranchTarget",
                onGetItems: () => GetBeatDropdownItems(beat),
                onValueChanged: (value) => OnBranchTargetChanged(branch, value, nodeView),
                allowCreateNew: false,
                onCreateNew: null
            );
            targetDropdown.Value = branch.NextBeatID ?? "";
            targetRow.Add(targetDropdown);

            // Add click to highlight target button
            Button highlightButton = new Button(() => HighlightTargetBeat(branch.NextBeatID));
            highlightButton.text = "Go to Target";
            highlightButton.style.marginTop = 4;
            targetRow.Add(highlightButton);

            branchItem.Add(targetRow);

            // Conditions section
            CreateConditionsUI(branch, branchItem, nodeView);

            return branchItem;
        }

        /// <summary>
        /// Sets up the user interface for managing conditions associated with a given BeatBranchSO object;
        /// includes a header section with a label and button to add new conditions, and a list section for displaying and managing existing conditions
        /// </summary>
        /// <param name="branch">The BeatBranchSO instance containing the conditions to be managed</param>
        /// <param name="parent">The parent VisualElement to which the conditions UI will be added</param>
        /// <param name="nodeView">The BeatNodeView representing the node in the editor where the conditions are displayed</param>
        private void CreateConditionsUI(BeatBranchSO branch, VisualElement parent, BeatNodeView nodeView)
        {
            // Conditions header
            VisualElement conditionsHeader = new VisualElement();
            conditionsHeader.style.flexDirection = FlexDirection.Row;
            conditionsHeader.style.justifyContent = Justify.SpaceBetween;
            conditionsHeader.style.alignItems = Align.Center;
            conditionsHeader.style.marginTop = 8;
            conditionsHeader.style.marginBottom = 4;
            conditionsHeader.style.borderTopWidth = 1;
            conditionsHeader.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
            conditionsHeader.style.paddingTop = 8;

            Label conditionsLabel = new Label("Conditions (all must be true)");
            conditionsLabel.style.fontSize = 11;
            conditionsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            conditionsHeader.Add(conditionsLabel);

            Button addConditionButton = new Button(() => ShowAddConditionMenu(branch, nodeView));
            addConditionButton.text = "+ Add";
            addConditionButton.style.paddingLeft = 6;
            addConditionButton.style.paddingRight = 6;
            addConditionButton.style.height = 18;
            conditionsHeader.Add(addConditionButton);

            parent.Add(conditionsHeader);

            // Conditions list
            SerializedObject branchSerialized = new SerializedObject(branch);
            SerializedProperty conditionsProperty = branchSerialized.FindProperty("conditions");

            // Check if there are any conditions
            if (conditionsProperty == null || conditionsProperty.arraySize == 0)
            {
                // If not, add a message
                Label noConditionsLabel = new Label("No conditions (always taken)");
                noConditionsLabel.style.color = new Color(0.5f, 0.7f, 0.5f);
                noConditionsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                noConditionsLabel.style.fontSize = 11;
                noConditionsLabel.style.marginLeft = 8;
                parent.Add(noConditionsLabel);
            }
            else
            {
                for (int i = 0; i < conditionsProperty.arraySize; i++)
                {
                    int condIndex = i;
                    SerializedProperty condProp = conditionsProperty.GetArrayElementAtIndex(i);
                    BranchConditionSO condition = condProp.objectReferenceValue as BranchConditionSO;

                    // Skip if the condition is null
                    if (!condition) continue;

                    VisualElement conditionRow = new VisualElement();
                    conditionRow.style.flexDirection = FlexDirection.Row;
                    conditionRow.style.justifyContent = Justify.SpaceBetween;
                    conditionRow.style.alignItems = Align.Center;
                    conditionRow.style.marginLeft = 8;
                    conditionRow.style.marginBottom = 2;

                    // Condition description
                    Label condDesc = new Label(condition.GetDescription());
                    condDesc.style.fontSize = 11;
                    condDesc.style.flexGrow = 1;
                    conditionRow.Add(condDesc);

                    // Edit button (select in inspector)
                    Button editButton = new Button(() => Selection.activeObject = condition);
                    editButton.text = "Edit";
                    editButton.style.width = 36;
                    editButton.style.height = 16;
                    editButton.style.marginRight = 2;
                    conditionRow.Add(editButton);

                    // Remove button
                    Button removeButton = new Button(() => RemoveConditionAtIndex(branch, condIndex, nodeView));
                    removeButton.text = "✕";
                    removeButton.style.width = 20;
                    removeButton.style.height = 16;
                    removeButton.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
                    conditionRow.Add(removeButton);

                    parent.Add(conditionRow);
                }
            }
        }

        /// <summary>
        /// Creates and configures the default "next beat" section for the specified SceneBeatSO and its associated BeatNodeView;
        /// this section includes a fallback dropdown for setting the default next beat, and a toggle for marking the beat as the end of a sequence
        /// </summary>
        /// <param name="beat">The SceneBeatSO instance being configured with its default "next beat" options</param>
        /// <param name="nodeView">The associated BeatNodeView that visually represents the beat in the editor</param>
        private void CreateDefaultNextBeatSection(SceneBeatSO beat, BeatNodeView nodeView)
        {
            VisualElement defaultSection = new VisualElement();
            defaultSection.style.marginTop = 12;
            defaultSection.style.paddingTop = 8;
            defaultSection.style.borderTopWidth = 1;
            defaultSection.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);

            Label defaultLabel = new Label("Fallback (if no branch matches)");
            defaultLabel.style.fontSize = 12;
            defaultLabel.style.marginBottom = 4;
            defaultLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            defaultSection.Add(defaultLabel);

            SearchableDropdown defaultDropdown = new SearchableDropdown(
                label: "Default Next Beat",
                prefsKey: "DefaultNextBeat",
                onGetItems: () => GetBeatDropdownItems(beat),
                onValueChanged: (value) => OnBeatPropertyChanged(beat, "defaultNextBeatID", value),
                allowCreateNew: false,
                onCreateNew: null
            );
            defaultDropdown.Value = beat.DefaultNextBeatID ?? "";
            defaultSection.Add(defaultDropdown);

            // Is End Beat toggle
            VisualElement endBeatRow = new VisualElement();
            endBeatRow.style.flexDirection = FlexDirection.Row;
            endBeatRow.style.alignItems = Align.Center;
            endBeatRow.style.marginTop = 8;

            Toggle endBeatToggle = new Toggle("Is End Beat");
            endBeatToggle.value = beat.IsEndBeat;
            endBeatToggle.RegisterValueChangedCallback(evt => OnEndBeatToggleChanged(beat, evt.newValue, nodeView));
            endBeatRow.Add(endBeatToggle);

            defaultSection.Add(endBeatRow);
            _inspectorContent.Add(defaultSection);
        }

        /// <summary>
        /// Constructs a list of dropdown items representing all SceneBeatSO assets within the currently selected SceneTemplateSO,
        /// excluding the specified beat. Each dropdown item contains the unique BeatID and display name of a SceneBeatSO
        /// </summary>
        /// <param name="excludeBeat">The SceneBeatSO to exclude from the dropdown list</param>
        /// <returns>A list of DropdownItem objects, each representing a valid SceneBeatSO that is part of the current SceneTemplateSO</returns>
        private List<SearchableDropdown.DropdownItem> GetBeatDropdownItems(SceneBeatSO excludeBeat)
        {
            List<SearchableDropdown.DropdownItem> items = new List<SearchableDropdown.DropdownItem>();

            // Exit case - there is no template set
            if (!_currentTemplate) return items;

            SerializedObject serialized = new SerializedObject(_currentTemplate);
            SerializedProperty beatsProperty = serialized.FindProperty("beats");

            // Exit case - there are no beats
            if (beatsProperty == null) return items;

            for (int i = 0; i < beatsProperty.arraySize; i++)
            {
                SerializedProperty beatProp = beatsProperty.GetArrayElementAtIndex(i);
                SceneBeatSO beat = beatProp.objectReferenceValue as SceneBeatSO;

                // Skip if the beat doesn't exist
                if (!beat) continue;
                
                // Skip if the beat is the one we're excluding
                if (beat == excludeBeat) continue;
                
                // Skip if the beat ID is empty
                if (string.IsNullOrEmpty(beat.BeatID)) continue;

                items.Add(new SearchableDropdown.DropdownItem(
                    id: beat.BeatID,
                    displayName: beat.name
                ));
            }

            return items;
        }

        /// <summary>
        /// Handles the event triggered when the "Add Branch" button is clicked;
        /// this method displays a context menu with a list of available beats to create branches for the provided SceneBeatSO,
        /// allowing the user to select and establish a new connection to another beat
        /// </summary>
        /// <param name="beat">The SceneBeatSO instance for which a new branch is being created</param>
        /// <param name="nodeView">The BeatNodeView instance associated with the provided beat, used for visual representation</param>
        private void OnAddBranchClicked(SceneBeatSO beat, BeatNodeView nodeView)
        {
            GenericMenu menu = new GenericMenu();
            List<SearchableDropdown.DropdownItem> beatItems = GetBeatDropdownItems(beat);

            // Check if there are any beats to add branches to
            if (beatItems.Count == 0)
            {
                // If not, add a message
                menu.AddDisabledItem(new GUIContent("No other beats available"));
            }
            else
            {
                // If there are, add a menu item for each beat
                for (int i = 0; i < beatItems.Count; i++)
                {
                    SearchableDropdown.DropdownItem item = beatItems[i];
                    menu.AddItem(new GUIContent(item.DisplayName), false, () =>
                    {
                        CreateNewBranch(beat, item.ID, nodeView);
                    });
                }
            }
            
            menu.ShowAsContext();
        }

        /// <summary>
        /// Creates a new branch in the Scene Template Editor by instantiating a new BeatBranchSO asset,
        /// linking it to the specified target beat, and adding it as a sub-asset to the current SceneTemplateSO;
        /// updates the branch properties, adds it to the source beat's branches, saves and refreshes the asset,
        /// and updates the user interface
        /// </summary>
        /// <param name="beat">
        /// The source SceneBeatSO to which the new branch will be linked
        /// </param>
        /// <param name="targetBeatID">
        /// The unique identifier of the target SceneBeatSO to which the branch will connect
        /// </param>
        /// <param name="nodeView">
        /// The BeatNodeView instance representing the source beat in the graph view; used for UI refresh
        /// </param
        private void CreateNewBranch(SceneBeatSO beat, string targetBeatID, BeatNodeView nodeView)
        {
            BeatBranchSO newBranch = CreateInstance<BeatBranchSO>();
            
            // Set the next beat ID
            SerializedObject branchSerialized = new SerializedObject(newBranch);
            SerializedProperty targetProp = branchSerialized.FindProperty("nextBeatID");
            if (targetProp != null)
            {
                targetProp.stringValue = targetBeatID;
                branchSerialized.ApplyModifiedProperties();
            }

            // Build the branch name
            StringBuilder nameBuilder = new StringBuilder();
            nameBuilder.Append("Branch_");
            nameBuilder.Append(beat.BeatID);
            nameBuilder.Append("_to_");
            nameBuilder.Append(targetBeatID);
            newBranch.name = nameBuilder.ToString();

            // Add as a sub-asset to the template
            string templatePath = AssetDatabase.GetAssetPath(_currentTemplate);
            AssetDatabase.AddObjectToAsset(newBranch, templatePath);

            // Add the branch to the beat's branches
            SerializedObject beatSerialized = new SerializedObject(beat);
            SerializedProperty branchesProperty = beatSerialized.FindProperty("branches");
            if (branchesProperty != null)
            {
                branchesProperty.InsertArrayElementAtIndex(branchesProperty.arraySize);
                SerializedProperty newElement = branchesProperty.GetArrayElementAtIndex(branchesProperty.arraySize - 1);
                newElement.objectReferenceValue = newBranch;
                beatSerialized.ApplyModifiedProperties();
            }

            // Save the asset
            EditorUtility.SetDirty(beat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Refresh the view
            InitializeGraphView();
            ShowBeatInspector(nodeView);
        }

        /// <summary>
        /// Moves a branch within the list of branches in a SceneBeatSO asset by adjusting its position index;
        /// this method ensures the branch is moved to the desired position, saves the changes, updates the inspector, and avoids out-of-bounds operations
        /// </summary>
        /// <param name="beat">The SceneBeatSO asset containing the list of branches to adjust</param>
        /// <param name="currentIndex">The current index of the branch to be moved</param>
        /// <param name="direction">The direction to move the branch; positive for forward, negative for backward</param>
        /// <param name="nodeView">The BeatNodeView associated with the branch, used to update the UI component</param>
        private void MoveBranch(SceneBeatSO beat, int currentIndex, int direction, BeatNodeView nodeView)
        {
            // Calculate the new index
            int newIndex = currentIndex + direction;

            // Get the beat's branches
            SerializedObject beatSerialized = new SerializedObject(beat);
            SerializedProperty branchesProperty = beatSerialized.FindProperty("branches");

            // Exit case - if there is no "branches" property
            if (branchesProperty == null) return;
            
            // Exit case - if the new index is out of bounds
            if (newIndex < 0 || newIndex >= branchesProperty.arraySize) return;

            // Move the branch
            branchesProperty.MoveArrayElement(currentIndex, newIndex);
            beatSerialized.ApplyModifiedProperties();

            // Save the asset
            EditorUtility.SetDirty(beat);
            AssetDatabase.SaveAssets();

            // Update the inspector
            ShowBeatInspector(nodeView);
        }

        /// <summary>
        /// Deletes a branch from the branches list of the specified SceneBeatSO asset based on the provided index;
        /// prompts the user for confirmation, removes the branch, applies and saves the updated asset, and refreshes the graph view and beat inspector
        /// </summary>
        /// <param name="beat">The SceneBeatSO asset from which the branch will be deleted</param>
        /// <param name="index">The index of the branch to delete in the branches array</param>
        /// <param name="nodeView">The BeatNodeView to refresh after the deletion</param>
        private void DeleteBranchAtIndex(SceneBeatSO beat, int index, BeatNodeView nodeView)
        {
            SerializedObject beatSerialized = new SerializedObject(beat);
            SerializedProperty branchesProperty = beatSerialized.FindProperty("branches");

            // Exit case - if there is no "branches" property or the index is out of bounds
            if (branchesProperty == null || index >= branchesProperty.arraySize) return;

            SerializedProperty branchProp = branchesProperty.GetArrayElementAtIndex(index);
            BeatBranchSO branch = branchProp.objectReferenceValue as BeatBranchSO;

            // Query the user to confirm the decision
            if (!EditorUtility.DisplayDialog(
                    "Delete Branch",
                    "Delete this branch?",
                    "Delete",
                    "Cancel"
                )
            ) return;

            // Remove the branch from the beat's branches'
            branchesProperty.DeleteArrayElementAtIndex(index);
            beatSerialized.ApplyModifiedProperties();

            // Destroy the branch if it exists
            if (branch)
            {
                AssetDatabase.RemoveObjectFromAsset(branch);
                DestroyImmediate(branch, true);
            }

            // Save the asset
            EditorUtility.SetDirty(beat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Refresh the view
            InitializeGraphView();
            ShowBeatInspector(nodeView);
        }

        /// <summary>
        /// Updates the target destination of a BeatBranchSO object when its branch target changes;
        /// applies the new target identifier, saves the changes to the branch asset, and refreshes the graph view to reflect the update
        /// </summary>
        /// <param name="branch">The BeatBranchSO object representing the branch whose target is being updated</param>
        /// <param name="newTargetID">The unique identifier of the new target beat to assign to the branch</param>
        /// <param name="nodeView">The BeatNodeView instance associated with the branch, used for visualization and editing within the editor</param>
        private void OnBranchTargetChanged(BeatBranchSO branch, string newTargetID, BeatNodeView nodeView)
        {
            SerializedObject branchSerialized = new SerializedObject(branch);
            SerializedProperty targetProp = branchSerialized.FindProperty("nextBeatID");

            // Update the target property if it exists
            if (targetProp != null)
            {
                targetProp.stringValue = newTargetID;
                branchSerialized.ApplyModifiedProperties();
            }

            // Save the asset
            EditorUtility.SetDirty(branch);
            AssetDatabase.SaveAssets();

            // Refresh the view
            InitializeGraphView();
        }

        /// <summary>
        /// Highlights a specified beat node in the graph view by its unique identifier;
        /// If the target beat ID matches a node in the graph, the node is selected; otherwise, a dialog is shown indicating the beat was not found
        /// </summary>
        /// <param name="beatID">The unique identifier of the beat to highlight within the graph view</param>
        private void HighlightTargetBeat(string beatID)
        {
            // Exit case - if the beat ID is empty
            if (string.IsNullOrEmpty(beatID)) return;

            // Find the beat with the given ID
            BeatNodeView targetNode = null;
            _graphContent.Query<BeatNodeView>().ForEach(node =>
            {
                if (node.Beat && node.Beat.BeatID == beatID)
                {
                    targetNode = node;
                }
            });

            // Select the node if it exists
            if (targetNode != null) SelectNode(targetNode);
            else EditorUtility.DisplayDialog("Not Found", "Target beat not found in graph.", "OK");
        }

        /// <summary>
        /// Displays a context menu for adding a condition to a specified branch in the SceneTemplate editor;
        /// the menu provides options to create a new BranchConditionSO asset or select an existing one
        /// </summary>
        /// <param name="branch">The branch entity (BeatBranchSO) to which the condition will be added</param>
        /// <param name="nodeView">The visual representation (BeatNodeView) of the branch node in the editor</param>
        private void ShowAddConditionMenu(BeatBranchSO branch, BeatNodeView nodeView)
        {
            GenericMenu menu = new GenericMenu();

            // Find all the branch conditions
            string[] guids = AssetDatabase.FindAssets("t:BranchConditionSO");
            
            if (guids.Length == 0)
            {
                // If there are no conditions found, add a menu item to create a new condition
                menu.AddItem(new GUIContent("Create New Condition Asset..."), false, () =>
                {
                    EditorUtility.DisplayDialog(
                        "Create Condition",
                        "Create a new BranchConditionSO asset in your project, then add it here.",
                        "OK"
                    );
                });
            }
            else
            {
                // If there are conditions found, add a menu item to select one
                menu.AddItem(new GUIContent("Select Existing Condition..."), false, () =>
                {
                    ShowConditionPickerWindow(branch, nodeView);
                });

                menu.AddSeparator("");

                // Add a menu item to select a new condition
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    BranchConditionSO condition = AssetDatabase.LoadAssetAtPath<BranchConditionSO>(path);

                    // Skip if the condition is null
                    if (!condition) continue;

                    string conditionName = condition.name;
                    menu.AddItem(new GUIContent(conditionName), false, () =>
                    {
                        AddConditionToBranch(branch, condition, nodeView);
                    });
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Displays the object picker window for selecting a BranchConditionSO instance;
        /// sets the provided BeatBranchSO and BeatNodeView instances as pending for condition assignment,
        /// and begins listening for updates via the Unity editor's update loop
        /// </summary>
        /// <param name="branch">The beat branch to associate with the selected condition</param>
        /// <param name="nodeView">The beat node view representing the graph node to associate with the condition</param>
        private void ShowConditionPickerWindow(BeatBranchSO branch, BeatNodeView nodeView)
        {
            EditorGUIUtility.ShowObjectPicker<BranchConditionSO>(null, false, "", 0);
            _pendingConditionBranch = branch;
            _pendingConditionNodeView = nodeView;
            EditorApplication.update += OnConditionPickerUpdate;
        }

        /// <summary>
        /// Monitors the update loop of the Object Picker window used for selecting a BranchConditionSO asset;
        /// this method handles the selection process and associates the chosen condition with a pending BeatBranchSO and BeatNodeView;
        /// upon selection or window closure, it finalizes the process, clears pending data, and detaches itself from the update event
        /// </summary
        private void OnConditionPickerUpdate()
        {
            // Exit case - if the picker window is closed
            if (EditorGUIUtility.GetObjectPickerControlID() != 0) return;
            
            EditorApplication.update -= OnConditionPickerUpdate;

            // Get the selected condition
            BranchConditionSO pickedCondition = EditorGUIUtility.GetObjectPickerObject() as BranchConditionSO;
            if (pickedCondition && _pendingConditionBranch)
            {
                AddConditionToBranch(_pendingConditionBranch, pickedCondition, _pendingConditionNodeView);
            }

            // Clear the pending variables
            _pendingConditionBranch = null;
            _pendingConditionNodeView = null;
        }

        /// <summary>
        /// Adds a condition to a specified branch within the SceneTemplateEditor;
        /// validates that the condition is not already present, applies the modification,
        /// saves the changes to the asset, and refreshes the beat inspector view
        /// </summary>
        /// <param name="branch">The target BeatBranchSO to which the condition will be added</param>
        /// <param name="condition">The BranchConditionSO to add to the branch</param>
        /// <param name="nodeView">The BeatNodeView associated with the branch, used to refresh the UI after the condition is added</param>
        private void AddConditionToBranch(BeatBranchSO branch, BranchConditionSO condition, BeatNodeView nodeView)
        {
            SerializedObject branchSerialized = new SerializedObject(branch);
            SerializedProperty conditionsProperty = branchSerialized.FindProperty("conditions");

            // Exit case - if there is no "conditions" property
            if (conditionsProperty == null) return;

            // Add a new condition element
            for (int i = 0; i < conditionsProperty.arraySize; i++)
            {
                SerializedProperty prop = conditionsProperty.GetArrayElementAtIndex(i);
                if (prop.objectReferenceValue == condition)
                {
                    EditorUtility.DisplayDialog("Already Added", "This condition is already on this branch.", "OK");
                    return;
                }
            }

            // Add the condition
            conditionsProperty.InsertArrayElementAtIndex(conditionsProperty.arraySize);
            SerializedProperty newElement = conditionsProperty.GetArrayElementAtIndex(conditionsProperty.arraySize - 1);
            newElement.objectReferenceValue = condition;
            branchSerialized.ApplyModifiedProperties();

            // Save the asset
            EditorUtility.SetDirty(branch);
            AssetDatabase.SaveAssets();

            // Refresh the view
            ShowBeatInspector(nodeView);
        }

        /// <summary>
        /// Removes a condition at the specified index from the given BeatBranchSO asset;
        /// Updates the serialized object, applies modifications, saves changes to the asset,
        /// and refreshes the associated BeatNodeView to reflect the update
        /// </summary>
        /// <param name="branch">The BeatBranchSO asset containing the conditions to modify</param>
        /// <param name="index">The index of the condition to be removed</param>
        /// <param name="nodeView">The BeatNodeView instance to be refreshed after the modification</param>
        private void RemoveConditionAtIndex(BeatBranchSO branch, int index, BeatNodeView nodeView)
        {
            SerializedObject branchSerialized = new SerializedObject(branch);
            SerializedProperty conditionsProperty = branchSerialized.FindProperty("conditions");

            // Exit case - if there is no "conditions" property or the index is out of bounds
            if (conditionsProperty == null || index >= conditionsProperty.arraySize) return;

            // Remove the condition
            conditionsProperty.DeleteArrayElementAtIndex(index);
            branchSerialized.ApplyModifiedProperties();

            // Save the asset
            EditorUtility.SetDirty(branch);
            AssetDatabase.SaveAssets();

            // Refresh the view
            ShowBeatInspector(nodeView);
        }

        /// <summary>
        /// Updates the "Is End Beat" status for a SceneBeatSO instance when the corresponding toggle is changed;
        /// modifies the serialized property, saves the changes to the asset, and refreshes the graph view to reflect updates
        /// </summary>
        /// <param name="beat">The SceneBeatSO instance representing the beat being modified</param>
        /// <param name="newValue">The new boolean value indicating whether the beat is an end beat</param>
        /// <param name="nodeView">The BeatNodeView associated with the beat, used for updating the visual representation</param>
        private void OnEndBeatToggleChanged(SceneBeatSO beat, bool newValue, BeatNodeView nodeView)
        {
            SerializedObject beatSerialized = new SerializedObject(beat);
            SerializedProperty endBeatProp = beatSerialized.FindProperty("isEndBeat");

            // Update the end beat property if it exists
            if (endBeatProp != null)
            {
                endBeatProp.boolValue = newValue;
                beatSerialized.ApplyModifiedProperties();
            }

            // Save the asset
            EditorUtility.SetDirty(beat);
            AssetDatabase.SaveAssets();

            // Refresh the view
            InitializeGraphView();
        }

        /// <summary>
        /// Handles changes to the search field input; updates the current search text
        /// and applies the search filter to the editor's content, enabling dynamic filtering
        /// of displayed elements based on the new search criteria
        /// </summary>
        /// <param name="evt">The event containing the updated search text value</param
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _currentSearchText = evt.newValue?.Trim().ToLower() ?? "";
            ApplySearchFilter();
        }

        /// <summary>
        /// Handles the event triggered when the "Clear Search" button is clicked;
        /// this method resets the search field's value, clears the current search text,
        /// and applies an updated filter with no active search input
        /// </summary>
        private void OnClearSearchClicked()
        {
            if (_searchField != null) _searchField.value = "";
            _currentSearchText = "";
            ApplySearchFilter();
        }

        /// <summary>
        /// Filters the nodes displayed in the graph view based on the current search text;
        /// clears the filter state if the search text is empty, and updates the state of each node based on whether it matches the search criteria
        /// </summary
        private void ApplySearchFilter()
        {
            // Exit case - if the search field is empty
            if (_graphView == null) return;
            
            // If the search is empty, clear all filter states
            if (string.IsNullOrEmpty(_currentSearchText))
            {
                _graphContent.Query<BeatNodeView>().ForEach(node => node.ClearFilterState());
                return;
            }

            // Apply filter to each node
            _graphContent.Query<BeatNodeView>().ForEach(node =>
            {
                // Exit case - if the node does not have a beat
                if (!node.Beat)
                {
                    node.SetFilterState(false);
                    return;
                }

                // Check if beat matches search
                bool matches = MatchesSearch(node.Beat, _currentSearchText);
                node.SetFilterState(matches, matches);
            });
        }

        /// <summary>
        /// Determines whether a given SceneBeatSO matches the provided search text;
        /// evaluates multiple properties of the SceneBeatSO, including BeatID, name, SpeakerRoleID, TargetRoleID,
        /// and VariationSetID, for a case-insensitive match
        /// </summary>
        /// <param name="beat">The SceneBeatSO instance to check against the search text</param>
        /// <param name="searchText">The case-insensitive search string to match against the SceneBeatSO's properties</param>
        /// <returns>True if any of the SceneBeatSO's properties match the search text; otherwise, false</returns>
        private bool MatchesSearch(SceneBeatSO beat, string searchText)
        {
            // Exit case - the beat does not exist
            if (!beat) return false;

            // Exit case - the beat ID exists and contains the search text
            if (!string.IsNullOrEmpty(beat.BeatID) && beat.BeatID.ToLower().Contains(searchText))
                return true;

            // Exit case - the beat asset exists (SceneBeatSO) and contains the search text
            if (!string.IsNullOrEmpty(beat.name) && beat.name.ToLower().Contains(searchText))
                return true;

            // Exit case - the speaker role exists and contains the search text
            if (!string.IsNullOrEmpty(beat.SpeakerRoleID) && beat.SpeakerRoleID.ToLower().Contains(searchText))
                return true;

            // Exit case - the target role exists and contains the search text
            if (!string.IsNullOrEmpty(beat.TargetRoleID) && beat.TargetRoleID.ToLower().Contains(searchText))
                return true;

            // Exit case - the variation set exists and contains the search text
            if (!string.IsNullOrEmpty(beat.VariationSetID) && beat.VariationSetID.ToLower().Contains(searchText))
                return true;

            return false;
        }

        /// <summary>
        /// Displays a dialog for creating a new SceneBeatSO asset when a drag action is performed and dropped onto an empty space;
        /// if the user confirms the creation, the method generates a new beat at the specified position, validates its ID,
        /// connects it to the source node, and updates the graph view
        /// </summary>
        /// <param name="sourceNode">The originating BeatNodeView from which the drag action began, representing the source of the connection</param>
        /// <param name="nodePosition">The position in the graph view where the new beat should be created</param>
        public void ShowCreateBeatFromDrag(BeatNodeView sourceNode, Vector2 nodePosition)
        {
            // Exit case - if the template or source node is null, or if the source node does not have a Beat
            if (!_currentTemplate || sourceNode == null || !sourceNode.Beat) return;
            
            // Show the beat creation dialog
            BeatCreationDialog.Show(
                onCreate: (beatID, displayName) =>
                {
                    // Create the new beat at the drop position
                    CreateBeat(beatID, displayName, nodePosition);
                    
                    // Find the newly created beat node and create a branch to it
                    _graphContent.Query<BeatNodeView>().ForEach(node =>
                    {
                        // Exit case - the node does not have a beat
                        if (!node.Beat) return;

                        // Exit case - the beat IDs are mismatching
                        if (node.Beat.BeatID != beatID) return;

                        CreateBranch(sourceNode, node);
                    });
                },
                onValidateID: IsBeatIDUnique
            );
        }

        /// <summary>
        /// Handles mouse wheel events for zooming in and out of the graph view;
        /// adjusts the zoom level based on the scroll direction and the wheel delta,
        /// and applies the zoom centered around the mouse cursor position;
        /// prevents the event from propagating further up the event chain
        /// </summary>
        /// <param name="evt">The wheel event containing information about the scroll delta and mouse position</param>
        private void OnMouseWheel(WheelEvent evt)
        {
            float zoomDelta = -evt.delta.y * ZoomStep * 0.1f;
            Vector2 mousePos = evt.localMousePosition;
            Zoom(zoomDelta, mousePos);
            evt.StopPropagation();
        }

        /// <summary>
        /// Initiates the panning action within the graph view when the left mouse button is pressed;
        /// this method sets the panning state, updates the last mouse position to the current event's position,
        /// and ensures the graph view captures the mouse input to track subsequent movements
        /// </summary>
        /// <param name="evt">The MouseDownEvent containing information about the mouse button press,
        /// including the button pressed and the position of the mouse</param>
        private void OnPanStart(MouseDownEvent evt)
        {
            // Exit case - the left mouse button is not pressed
            if (evt.button != 0) return;

            // Exit case - clicking on a node
            BeatNodeView clickedNode = FindNodeAtPosition(evt.mousePosition);
            if (clickedNode != null) return;
            
            // Exit case - clicking on a hovered connection
            if(_hoveredConnection != null) return;
            
            // Exit case - clicking on zoom controls
            VisualElement clicked = _graphView.panel.Pick(evt.mousePosition);
            if (clicked != null)
            {
                VisualElement current = clicked;
                while (current != null)
                {
                    if (current.name == "ZoomControlsContainer") return;
                    current = current.parent;
                }
            }
            
            _isPanning = true;
            _lastMousePosition = evt.mousePosition;
            _graphView.CaptureMouse();
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the mouse move event during a pan operation in the graph view;
        /// updates the pan offset based on the mouse movement delta, updates the last mouse position,
        /// and re-applies the zoom and pan transformation to the graph content
        /// </summary>
        /// <param name="evt">The mouse move event containing information about the current mouse position</param>
        private void OnPanMove(MouseMoveEvent evt)
        {
            // Exit case - not panning
            if (!_isPanning) return;

            Vector2 delta = evt.mousePosition - _lastMousePosition;
            _panOffset += delta;
            _lastMousePosition = evt.mousePosition;

            ApplyZoomAndPan();
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the event triggered when a pan gesture ends within the graph view;
        /// this method stops the panning operation, releases the mouse capture,
        /// and prevents further propagation of the event
        /// </summary>
        /// <param name="evt">The mouse event data containing information about the button and mouse</param>
        private void OnPanEnd(MouseUpEvent evt)
        {
            // Exit case - not the left mouse button
            if(evt.button != 0) return;
            
            // Exit case - not panning
            if(!_isPanning) return;
            
            _isPanning = false;
            _graphView.ReleaseMouse();
            evt.StopPropagation();
        }

        /// <summary>
        /// Adjusts the zoom level of the editor window and optionally repositions the view
        /// to zoom towards a specified focal point
        /// </summary>
        /// <param name="delta">The amount to change the current zoom level, positive to zoom in and negative to zoom out</param>
        /// <param name="centerPoint">An optional focal point in local coordinates; if specified, the view will pan to maintain focus on this point during zoom</param>
        private void Zoom(float delta, Vector2? centerPoint)
        {
            float oldZoom = _zoomLevel;
            _zoomLevel = Mathf.Clamp(_zoomLevel + delta, MinZoom, MaxZoom);
            
            // If there's a center point, adjust pan to zoom toward that point
            if(centerPoint.HasValue && Mathf.Abs(_zoomLevel - oldZoom) > 0.001f) 
            {
                float zoomRatio = _zoomLevel / oldZoom;
                Vector2 pointInContent = (centerPoint.Value - _panOffset) / oldZoom;
                _panOffset = centerPoint.Value - (pointInContent * _zoomLevel);
            }
            
            ApplyZoomAndPan();
            UpdateZoomLevelLabel();
        }

        /// <summary>
        /// Resets the editor view to its default state by restoring the zoom level to the original value
        /// and centering the pan offset; updates the UI to reflect the reset zoom level
        /// </summary>
        private void ResetView()
        {
            _zoomLevel = 1f;
            _panOffset = Vector2.zero;
            ApplyZoomAndPan();
            UpdateZoomLevelLabel();
        }

        /// <summary>
        /// Adjusts the visual representation of the graph content by applying current zoom and pan settings;
        /// Updates the translation and scale transformations of the graph content element to reflect the current
        /// pan offset and zoom level; prevents further processing if the graph content element is not initialized
        /// </summary>
        private void ApplyZoomAndPan()
        {
            // Exit case - the graph content does not exist
            if (_graphContent == null) return;
            
            // Apply the translation
            _graphContent.style.translate = new Translate(_panOffset.x, _panOffset.y);
            
            // Apply the zoom
            _graphContent.style.scale = new Scale(new Vector3(_zoomLevel, _zoomLevel, 1f));
        }

        /// <summary>
        /// Updates the zoom level label by calculating the current zoom level percentage,
        /// converting it to a string representation, and assigning it to the text property
        /// of the label UI element if it exists; ignores the operation if the label is null
        /// </summary>
        private void UpdateZoomLevelLabel()
        {
            // Exit case - the zoom level label does not exist
            if (_zoomLevelLabel == null) return;
            
            // Get the percentage
            int percentage = Mathf.RoundToInt(_zoomLevel * 100f);
            
            // Construct the label
            StringBuilder labelBuilder = new StringBuilder();
            labelBuilder.Append(percentage);
            labelBuilder.Append("%");

            // Set the label
            _zoomLevelLabel.text = labelBuilder.ToString();
        }

        /// <summary>
        /// Creates a UI element that allows for selecting a role from a dropdown with an additional option to edit or create a new role;
        /// the dropdown supports search functionality and is dynamically populated with available roles
        /// </summary>
        /// <param name="label">Label text displayed next to the dropdown menu</param>
        /// <param name="prefsKey">Key used to persist user preferences related to the dropdown selection</param>
        /// <param name="currentValue">The currently selected value in the dropdown</param>
        /// <param name="onValueChanged">Callback triggered when the dropdown value changes</param>
        /// <param name="onCreateNew">Callback triggered when a new role is created through the UI</param>
        /// <param name="nodeView">The beat node context to which the dropdown is related</param>
        /// <returns>A VisualElement containing the dropdown, an edit button, and optionally an inline role editor</returns>
        private VisualElement CreateRoleFieldWithEdit(
            string label,
            string prefsKey,
            string currentValue,
            Action<string> onValueChanged,
            Action<string> onCreateNew,
            BeatNodeView nodeView
        )
        {
            VisualElement container = new VisualElement();

            // Row for dropdown + edit button
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            SearchableDropdown dropdown = new SearchableDropdown(
                label: label,
                prefsKey: prefsKey,
                onGetItems: GetRoleDropdownItems,
                onValueChanged: onValueChanged,
                allowCreateNew: true,
                onCreateNew: (searchText) => CreateNewRole(searchText, (roleID) =>
                {
                    onCreateNew?.Invoke(roleID);
                })
            );
            dropdown.Value = currentValue ?? "";
            dropdown.style.flexGrow = 1;
            row.Add(dropdown);

            // Edit button
            Button editButton = new Button();
            editButton.text = "Edit";
            editButton.style.marginLeft = 4;
            editButton.style.width = 40;
            editButton.style.alignSelf = Align.FlexStart;
            row.Add(editButton);

            container.Add(row);

            // Placeholder for inline editor
            VisualElement editorContainer = new VisualElement();
            container.Add(editorContainer);

            // Track if editor is open
            bool isEditorOpen = false;

            editButton.clicked += () =>
            {
                if (isEditorOpen)
                {
                    // Close editor
                    editorContainer.Clear();
                    editButton.text = "Edit";
                    isEditorOpen = false;
                }
                else
                {
                    // Open editor
                    string roleID = dropdown.Value;
                    if (!string.IsNullOrEmpty(roleID))
                    {
                        VisualElement editor = CreateInlineRoleEditor(roleID, () =>
                        {
                            editorContainer.Clear();
                            editButton.text = "Edit";
                            isEditorOpen = false;
                            ShowBeatInspector(nodeView); // Refresh to show updated values
                        });
                        editorContainer.Add(editor);
                        editButton.text = "Close";
                        isEditorOpen = true;
                    }
                }
            };

            return container;
        }
        
        /// <summary>
        /// Creates an inline role editor for a specified role, providing the ability to make edits within the context of the associated scene template;
        /// A callback action is invoked when the editor is closed
        /// </summary>
        /// <param name="roleID">The unique identifier of the role to be edited</param>
        /// <param name="onClose">An action to be executed when the inline editor is closed</param>
        /// <returns>A VisualElement instance representing the inline role editor UI</returns>
        private VisualElement CreateInlineRoleEditor(string roleID, Action onClose)
        {
            // Find the role asset
            SceneRoleSO roleAsset = null;
            List<SceneRoleSO> allRoles = AssetCreator.FindAllAssets<SceneRoleSO>();
            for (int i = 0; i < allRoles.Count; i++)
            {
                // Skip if the role doesn't exist or the role doesn't match the specified ID
                if (!allRoles[i] || allRoles[i].RoleID != roleID) continue;
                
                roleAsset = allRoles[i];
                break;
            }

            // Exit case - role not found
            if (!roleAsset)
            {
                Label errorLabel = new Label("Role not found");
                errorLabel.style.color = new Color(1f, 0.4f, 0.4f);
                return errorLabel;
            }

            SerializedObject serializedRole = new SerializedObject(roleAsset);
            
            // Container for the inline editor
            VisualElement container = new VisualElement();
            container.style.marginLeft = 12;
            container.style.marginTop = 4;
            container.style.marginBottom = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            container.style.borderLeftWidth = 2;
            container.style.borderLeftColor = new Color(0.4f, 0.6f, 0.8f);

            // Header with close button
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 8;

            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("Editing Role: ");
            headerBuilder.Append(roleAsset.DisplayName ?? roleID);
            Label headerLabel = new Label(headerBuilder.ToString());
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(headerLabel);

            Button closeButton = new Button(() => onClose?.Invoke());
            closeButton.text = "X";
            closeButton.style.width = 20;
            closeButton.style.height = 20;
            header.Add(closeButton);

            container.Add(header);

            // Role ID field
            TextField roleIDField = new TextField("Role ID");
            roleIDField.value = roleAsset.RoleID ?? "";
            roleIDField.RegisterValueChangedCallback(evt =>
            {
                SerializedProperty property = serializedRole.FindProperty("roleID");
                
                // Exit case - if the property doesn't exist
                if (property == null) return;
                
                property.stringValue = evt.newValue;
                serializedRole.ApplyModifiedProperties();
                EditorUtility.SetDirty(roleAsset);
                AssetDatabase.SaveAssets();
            });
            container.Add(roleIDField);

            // Display Name field
            TextField displayNameField = new TextField("Display Name");
            displayNameField.value = roleAsset.DisplayName ?? "";
            displayNameField.RegisterValueChangedCallback(evt =>
            {
                SerializedProperty property = serializedRole.FindProperty("displayName");
                
                // Exit case - if the property is not found
                if (property == null) return;
                
                property.stringValue = evt.newValue;
                serializedRole.ApplyModifiedProperties();
                EditorUtility.SetDirty(roleAsset);
                AssetDatabase.SaveAssets();
            });
            container.Add(displayNameField);

            // Required Traits section
            Label requiredLabel = new Label("Required Traits (comma-separated IDs)");
            requiredLabel.style.marginTop = 8;
            requiredLabel.style.fontSize = 11;
            requiredLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(requiredLabel);

            string requiredTraitsStr = roleAsset.RequiredTraitIDs != null 
                ? string.Join(", ", roleAsset.RequiredTraitIDs)
                : "";
            TextField requiredTraitsField = new TextField();
            requiredTraitsField.value = requiredTraitsStr;
            requiredTraitsField.RegisterValueChangedCallback(evt =>
            {
                SerializedProperty property = serializedRole.FindProperty("requiredTraitIDs");
                
                // Exit case - the property doesn't exist
                if (property == null) return;
                
                string[] traits = evt.newValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                property.arraySize = traits.Length;
                for (int i = 0; i < traits.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).stringValue = traits[i].Trim();
                }
                serializedRole.ApplyModifiedProperties();
                EditorUtility.SetDirty(roleAsset);
                AssetDatabase.SaveAssets();
            });
            container.Add(requiredTraitsField);

            // Forbidden Traits section
            Label forbiddenLabel = new Label("Forbidden Traits (comma-separated IDs)");
            forbiddenLabel.style.marginTop = 8;
            forbiddenLabel.style.fontSize = 11;
            forbiddenLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(forbiddenLabel);

            string forbiddenTraitsStr = roleAsset.ForbiddenTraitIDs != null
                ? string.Join(", ", roleAsset.ForbiddenTraitIDs)
                : "";
            TextField forbiddenTraitsField = new TextField();
            forbiddenTraitsField.value = forbiddenTraitsStr;
            forbiddenTraitsField.RegisterValueChangedCallback(evt =>
            { 
                SerializedProperty property = serializedRole.FindProperty("forbiddenTraitIDs");

                // Exit case - the property doesn't exist
                if (property == null) return;
                
                string[] traits = evt.newValue.Split(new[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                property.arraySize = traits.Length;
                for (int i = 0; i < traits.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).stringValue = traits[i].Trim();
                }
                serializedRole.ApplyModifiedProperties();
                EditorUtility.SetDirty(roleAsset);
                AssetDatabase.SaveAssets();
            });
            container.Add(forbiddenTraitsField);

            return container;
        }

        /// <summary>
        /// Handles the event triggered when the "Rename Template" button is clicked;
        /// opens a dialog to rename the currently selected SceneTemplateSO asset;
        /// updates the display name and identifier of the template based on the new name input;
        /// saves the changes, ensures the asset is marked as dirty, and refreshes related UI
        /// </summary>
        private void OnRenameTemplateClicked() 
        {
            // Exit case - no template selected
            if(!_currentTemplate) return;
            
            // Show a simple input dialog
            string currentName = _currentTemplate.DisplayName ?? _currentTemplate.name;

            RenameTemplateDialog.Show(currentName, (newDisplayName) =>
            {
                // Exit case - if the new display name is empty
                if (string.IsNullOrEmpty(newDisplayName)) return;

                // Calculate new ID
                string oldID = _currentTemplate.ID;
                string newID = newDisplayName.ToLower().Replace(" ", "-");
                
                // Migrate node positions before changing the ID
                MigrateNodePositions(oldID, newID);
                
                SerializedObject serializedTemplate = new SerializedObject(_currentTemplate);

                // Update display name
                SerializedProperty displayNameProp = serializedTemplate.FindProperty("displayName");
                if (displayNameProp != null)
                {
                    displayNameProp.stringValue = newDisplayName;
                }

                // Update ID (convert to lowercase with dashes)
                SerializedProperty idProp = serializedTemplate.FindProperty("id");
                if (idProp != null)
                {
                    idProp.stringValue = newDisplayName.ToLower().Replace(" ", "-");
                }

                serializedTemplate.ApplyModifiedProperties();

                // Save the asset
                EditorUtility.SetDirty(_currentTemplate);
                AssetDatabase.SaveAssets();

                // Update the display
                UpdateTemplateSelectorDisplay();
                UpdateRecentTemplatesUI();
            });
        }

        /// <summary>
        /// Migrates node positions associated with a specific SceneTemplate from one template ID to another;
        /// updates the stored positions for all beats within the currently active SceneTemplate;
        /// skips migration if the template IDs are identical, no template is selected, or no beats exist in the template
        /// </summary>
        /// <param name="oldTemplateID">The ID of the existing SceneTemplate from which positions are being migrated</param>
        /// <param name="newTemplateID">The ID of the target SceneTemplate to which positions are being saved</param>
        private void MigrateNodePositions(string oldTemplateID, string newTemplateID)
        {
            // Exit case - IDs are the same
            if(oldTemplateID == newTemplateID) return;
            
            // Exit case - no template is selected
            if(_currentTemplate) return;
            
            IReadOnlyDictionary<string, ISceneBeat> beats = _currentTemplate.Beats;
            
            // Exit case - no beats exist inside the template
            if(beats == null) return;
            
            // Migrate each beat's position
            foreach(KeyValuePair<string, ISceneBeat> kvp in beats) 
            {
                string beatID = kvp.Key;
                
                // Load the position with the old key
                Vector2 nodePosition = NodePositionStorage.LoadPosition(oldTemplateID, beatID, Vector2.zero);
                
                // Skip if there was no saved position
                if (nodePosition == Vector2.zero) continue;
                
                // Save with the new key
                NodePositionStorage.SavePosition(newTemplateID, beatID, nodePosition);
                
                // Delete the old key
                NodePositionStorage.DeletePosition(oldTemplateID, beatID);
            }
        }

        /// <summary>
        /// Validates the references of a given SceneBeatSO object based on its associated identifiers and connections;
        /// this method checks if the beat references existing speaker roles, target roles, variation sets,
        /// branch targets, and default next beats within the scene template
        /// </summary>
        /// <param name="beat">The SceneBeatSO object to validate</param>
        /// <returns>A list of warning messages describing invalid or missing references in the provided beat</returns>
        private List<string> ValidateBeatReferences(SceneBeatSO beat)
        {
            List<string> warnings = new List<string>();

            // Exit case - if there is no beat to validate
            if (!beat) return warnings;

            // Cache available assets for efficiency
            List<SceneRoleSO> allRoles = AssetCreator.FindAllAssets<SceneRoleSO>();
            List<VariationSetSO> allVariationSets = AssetCreator.FindAllAssets<VariationSetSO>();

            // Check speaker role
            if (!string.IsNullOrEmpty(beat.SpeakerRoleID))
            {
                bool found = false;
                for (int i = 0; i < allRoles.Count; i++)
                {
                    // Skip if the role doesn't exist or the role doesn't match the specified ID
                    if (!allRoles[i] || allRoles[i].RoleID != beat.SpeakerRoleID) continue;
                    
                    found = true;
                    break;
                }
                if (!found)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Speaker role '");
                    sb.Append(beat.SpeakerRoleID);
                    sb.Append("' not found");
                    warnings.Add(sb.ToString());
                }
            }

            // Check target role
            if (!string.IsNullOrEmpty(beat.TargetRoleID))
            {
                bool found = false;
                for (int i = 0; i < allRoles.Count; i++)
                {
                    // Skip if the role doesn't exist or the role doesn't match the specified ID
                    if (!allRoles[i] || allRoles[i].RoleID != beat.TargetRoleID) continue;
                    
                    found = true;
                    break;
                }
                if (!found)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Target role '");
                    sb.Append(beat.TargetRoleID);
                    sb.Append("' not found");
                    warnings.Add(sb.ToString());
                }
            }

            // Check variation set
            if (!string.IsNullOrEmpty(beat.VariationSetID))
            {
                bool found = false;
                for (int i = 0; i < allVariationSets.Count; i++)
                {
                    // Skip if the variation set doesn't exist or the variation set doesn't match the specified ID
                    if (!allVariationSets[i] || allVariationSets[i].ID != beat.VariationSetID) continue;
                    
                    found = true;
                    break;
                }
                if (!found)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Variation set '");
                    sb.Append(beat.VariationSetID);
                    sb.Append("' not found");
                    warnings.Add(sb.ToString());
                }
            }
            
            // Check branch targets
            if (beat.Branches != null)
            {
                IReadOnlyDictionary<string, ISceneBeat> templateBeats = _currentTemplate?.Beats;

                for (int i = 0; i < beat.Branches.Count; i++)
                {
                    IBeatBranch branch = beat.Branches[i];
                    
                    // Skip if the branch doesn't exist
                    if (branch == null) continue;

                    string targetID = branch.NextBeatID;
                    
                    // Skip if the branch target is valid
                    if (!string.IsNullOrEmpty(targetID) && templateBeats != null && templateBeats.ContainsKey(targetID)) 
                        continue;
                    
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Branch target '");
                    sb.Append(targetID);
                    sb.Append("' not found");
                    warnings.Add(sb.ToString());
                }
            }

            // Check default next beat
            if (!string.IsNullOrEmpty(beat.DefaultNextBeatID) && !beat.IsEndBeat)
            {
                IReadOnlyDictionary<string, ISceneBeat> templateBeats = _currentTemplate?.Beats;
        
                // Skip if the template beats contains the next beat
                if (templateBeats != null && templateBeats.ContainsKey(beat.DefaultNextBeatID)) return warnings;
                
                StringBuilder sb = new StringBuilder();
                sb.Append("Default next beat '");
                sb.Append(beat.DefaultNextBeatID);
                sb.Append("' not found");
                warnings.Add(sb.ToString());
            }

            return warnings;
        }
        
        /// <summary>
        /// Refresh the Scene Template Editor Window when asset changes are detected
        /// </summary> 
        public void OnExternalAssetChange() 
        {
            // Exit case - no template loaded
            if(_currentTemplate) return;
            
            // Refresh the graph view
            InitializeGraphView();
            
            // Exit case - no node is selected
            if(_selectedNode == null) return;
            
            // Update the inspector
            ShowBeatInspector(_selectedNode);      
        }

        /// <summary>
        /// Handles the event triggered when the "Batch Creator" button is clicked;
        /// opens the BatchAssetCreatorWindow, allowing the user to create multiple Calliope assets in bulk
        /// </summary>
        private void OnBatchCreatorClicked() => BatchAssetCreatorWindow.ShowWindow();
    }
}