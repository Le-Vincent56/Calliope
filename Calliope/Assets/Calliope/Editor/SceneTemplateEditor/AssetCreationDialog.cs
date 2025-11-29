using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Reusable dialog window for creating new Calliope assets with customizable fields
    /// </summary>
    public class AssetCreationDialog : EditorWindow
    {
        /// <summary>
        /// Configuration for a field in the dialog
        /// </summary>
        public class FieldConfig
        {
            public string Key { get; set; }
            public string Label { get; set; }
            public string DefaultValue { get; set; }
            public bool Required { get; set; }
            public bool AutoGenerateFromName { get; set; }
            public Func<string, string> TransformForID { get; set; }

            public FieldConfig(
                string key, 
                string label, 
                bool required = false, 
                bool autoGenerateFromName = false,
                Func<string, string> transformForID = null
            )
            {
                Key = key;
                Label = label;
                Required = required;
                AutoGenerateFromName = autoGenerateFromName;
                TransformForID = transformForID;
            }
        }
        
        private string _assetTypeName;
        private string _defaultFolder;
        private List<FieldConfig> _fields;
        private Action<Dictionary<string, string>, string> _onCreate;
        private Dictionary<string, TextField> _textFields;
        private TextField _fileNameField;
        private Label _pathPreviewLabel;
        private Button _createButton;
        private Button _browseButton;
        private Label _errorLabel;
        private string _selectedFolder;

        public static void Show(
            string assetTypeName, 
            string defaultFolder, 
            List<FieldConfig> fields,
            Action<Dictionary<string, string>, string> onCreate
        )
        {
            StringBuilder titleBuilder = new StringBuilder();
            titleBuilder.Append("Create new ");
            titleBuilder.Append(assetTypeName);

            AssetCreationDialog window = GetWindow<AssetCreationDialog>(true, titleBuilder.ToString(), true);
            window.minSize = new Vector2(450, 250 + (fields.Count * 30));
            window.maxSize = new Vector2(450, 250 + (fields.Count * 30));
            window._assetTypeName = assetTypeName;
            window._defaultFolder = defaultFolder;
            window._selectedFolder = defaultFolder;
            window._fields = fields;
            window._onCreate = onCreate;
            window._textFields = new Dictionary<string, TextField>();
            window.CreateGUI();
        }

        /// <summary>
        /// Initializes and constructs the graphical user interface elements
        /// for the asset creation dialog, setting up layouts, controls, and
        /// event bindings to facilitate user interactions and ensure a smooth
        /// workflow during asset creation
        /// </summary>
        private void CreateGUI()
        {
            // Exit case - no fields are configured
            if (_fields == null) return;
            
            // Clear the existing content
            rootVisualElement.Clear();
            
            // Root container
            VisualElement root = rootVisualElement;
            root.style.paddingTop = new StyleLength(16);
            root.style.paddingLeft = new StyleLength(16);
            root.style.paddingBottom = new StyleLength(16);
            root.style.paddingRight = new StyleLength(16);
            
            StringBuilder labelBuilder = new StringBuilder();
            labelBuilder.Append("Create new ");
            labelBuilder.Append(_assetTypeName);
            
            // Title
            Label titleLabel = new Label(labelBuilder.ToString());
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 16;
            root.Add(titleLabel);
            
            // File name field
            VisualElement fileNameRow = new VisualElement();
            fileNameRow.style.flexDirection = FlexDirection.Row;
            fileNameRow.style.marginBottom = 12;
            
            Label fileNameLabel = new Label("File Name");
            fileNameLabel.style.minWidth = 100;
            fileNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            fileNameRow.Add(fileNameLabel);
            
            _fileNameField = new TextField();
            _fileNameField.style.flexGrow = 1;
            _fileNameField.RegisterValueChangedCallback(evt => OnFileNameChanged(evt.newValue));
            fileNameRow.Add(_fileNameField);
            root.Add(fileNameRow);
            
            // Dynamic fields
            for (int i = 0; i < _fields.Count; i++)
            {
                FieldConfig field = _fields[i];

                // Create the field row
                VisualElement fieldRow = new VisualElement();
                fieldRow.style.flexDirection = FlexDirection.Row;
                fieldRow.style.marginBottom = 8;

                // Create the label
                labelBuilder.Clear();
                labelBuilder.Append(field.Label);
                labelBuilder.Append(field.Required ? "*" : "");
                
                Label fieldLabel = new Label(labelBuilder.ToString());
                fieldLabel.style.minWidth = 100;
                fieldLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                fieldRow.Add(fieldLabel);

                // Create the input field
                TextField textField = new TextField();
                textField.style.flexGrow = 1;
                textField.value = field.DefaultValue ?? "";
                textField.RegisterValueChangedCallback(evt => ValidateFields());

                // Disable if auto-generating from name
                if (field.AutoGenerateFromName)
                {
                    textField.SetEnabled(false);
                    textField.style.opacity = 0.7f;
                }
                
                fieldRow.Add(textField);
                root.Add(fieldRow);

                // Cache the text field
                _textFields[field.Key] = textField;
            }

            // Create the location section
            VisualElement locationSection = new VisualElement();
            locationSection.style.marginTop = 12;
            locationSection.style.marginBottom = 12;
            
            // Create the label
            Label locationLabel = new Label("Save Location");
            locationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            locationLabel.style.marginBottom = 4;
            locationSection.Add(locationLabel);
            
            VisualElement locationRow = new VisualElement();
            locationRow.style.flexDirection = FlexDirection.Row;

            // Create the path preview label
            _pathPreviewLabel = new Label(_selectedFolder);
            _pathPreviewLabel.style.flexGrow = 1;
            _pathPreviewLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _pathPreviewLabel.style.overflow = Overflow.Hidden;
            _pathPreviewLabel.style.textOverflow = TextOverflow.Ellipsis;
            locationRow.Add(_pathPreviewLabel);
            
            // Create the browse button
            _browseButton = new Button(OnBrowseClicked);
            _browseButton.text = "Browse..";
            _browseButton.style.marginLeft = 8;
            locationRow.Add(_browseButton);
            
            locationSection.Add(locationRow);
            root.Add(locationSection);
            
            // Error label
            _errorLabel = new Label("");
            _errorLabel.style.color = new Color(1f, 0.3f, 0.3f);
            _errorLabel.style.marginBottom = 12;
            _errorLabel.style.display = DisplayStyle.None;
            root.Add(_errorLabel);
            
            // Button container
            VisualElement buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.FlexEnd;
            buttonContainer.style.marginTop = 8;

            // Cancel button
            Button cancelButton = new Button(Close);
            cancelButton.text = "Cancel";
            cancelButton.style.marginRight = 8;
            cancelButton.style.minWidth = 80;
            buttonContainer.Add(cancelButton);

            // Create button
            _createButton = new Button(OnCreateClicked);
            _createButton.text = "Create";
            _createButton.style.minWidth = 80;
            _createButton.SetEnabled(false);
            buttonContainer.Add(_createButton);

            root.Add(buttonContainer);

            // Focus the file name field
            _fileNameField.schedule.Execute(() => _fileNameField.Focus());
        }

        /// <summary>
        /// Handles updates to the file name entered in the asset creation dialog,
        /// propagates changes to auto-generated fields associated with the file name,
        /// and refreshes the path preview and validation status to ensure consistent
        /// and accurate information is displayed to the user
        /// </summary>
        /// <param name="fileName">The new file name entered by the user</param>
        private void OnFileNameChanged(string fileName)
        {
            // Update auto-generated fields
            for (int i = 0; i < _fields.Count; i++)
            {
                FieldConfig field = _fields[i];

                // Skip if the field is not autogenerated or the text field doesn't exist
                if (!field.AutoGenerateFromName || !_textFields.TryGetValue(field.Key, out TextField textField)) 
                    continue;
                
                // Generate the value
                string generatedValue = fileName;

                // Apply any custom transformation
                if (field.TransformForID != null)
                    generatedValue = field.TransformForID(fileName);

                textField.SetValueWithoutNotify(generatedValue);
            }

            // Update path preview
            UpdatePathPreview();

            // Validate
            ValidateFields();
        }

        /// <summary>
        /// Updates the path preview displayed in the asset creation dialog by constructing
        /// and formatting the full file path based on the current folder selection and the
        /// entered file name; ensures that the path is displayed accurately even when
        /// the file name is empty by showing only the selected folder
        /// </summary>
        private void UpdatePathPreview()
        {
            string fileName = _fileNameField.value?.Trim();

            if (string.IsNullOrEmpty(fileName))
            {
                _pathPreviewLabel.text = _selectedFolder;
            }
            else
            {
                StringBuilder pathBuilder = new StringBuilder();
                pathBuilder.Append(_selectedFolder);
                pathBuilder.Append("/");
                pathBuilder.Append(fileName);
                _pathPreviewLabel.text = pathBuilder.ToString();
            }
        }

        /// <summary>
        /// Validates the input fields within the asset creation dialog by checking the file name,
        /// constructed file path, and any other required fields; disables the creation button
        /// and displays error messages if the input fields are invalid or incomplete;
        /// ensures that the file name is not empty, that the specified file does not already exist,
        /// and that all required fields satisfy their constraints; updates the error label
        /// and its visibility based on validation results
        /// </summary>
        private void ValidateFields()
        {
            string fileName = _fileNameField.value?.Trim();

            // Exit case - the file name doesn't exist
            if (string.IsNullOrEmpty(fileName))
            {
                _createButton.SetEnabled(false);
                _errorLabel.style.display = DisplayStyle.None;
                return;
            }
            
            // Build the full path
            StringBuilder fullPathBuilder = new StringBuilder();
            fullPathBuilder.Append(_selectedFolder);
            fullPathBuilder.Append("/");
            fullPathBuilder.Append(fileName);
            fullPathBuilder.Append(".asset");
            string fullPath = fullPathBuilder.ToString();
            
            // Exit case - the file already exists
            if (File.Exists(fullPath))
            {
                _createButton.SetEnabled(false);
                _errorLabel.text = "A file with this name already exists";
                _errorLabel.style.display = DisplayStyle.Flex;
                return;
            }
            
            StringBuilder errorBuilder = new StringBuilder();

            // Check required fields
            for (int i = 0; i < _fields.Count; i++)
            {
                FieldConfig field = _fields[i];

                // Skip if the field is not required or the text field doesn't exist
                if (!field.Required || !_textFields.TryGetValue(field.Key, out TextField textField)) 
                    continue;
                
                // Skip if the field is not empty
                if (!string.IsNullOrEmpty(textField.value?.Trim())) continue;
                
                // Construct the error message 
                errorBuilder.Clear();
                errorBuilder.Append(field.Label);
                errorBuilder.Append(" is required");
                    
                _createButton.SetEnabled(false);
                _errorLabel.text = errorBuilder.ToString(); 
                _errorLabel.style.display = DisplayStyle.Flex;
                return;
            }

            // All valid
            _createButton.SetEnabled(true);
            _errorLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Handles the "Browse" button click event in the asset creation dialog;
        /// opens a folder selection dialog, validates the selected folder to ensure
        /// it is within the Unity "Assets" directory, updates the internal folder path,
        /// and refreshes the folder path preview; if the selected folder is invalid,
        /// displays an error dialog to notify the user
        /// </summary>
        private void OnBrowseClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel(
                    "Select Save Location",
                    _selectedFolder,
                    ""
            );

            // Exit case - no path selected
            if (string.IsNullOrEmpty(selectedPath)) return;
            
            // Convert to the relative path if inside Assets
            if (selectedPath.StartsWith(Application.dataPath))
            {
                // Set the selected folder
                StringBuilder folderPath = new StringBuilder();
                folderPath.Append("Assets");
                folderPath.Append(selectedPath.Substring(Application.dataPath.Length));
                _selectedFolder = folderPath.ToString();
            }
            else
            {
                // Notify invalid location
                EditorUtility.DisplayDialog("Invalid Location", "Please select a folder inside the Assets directory.", "OK");
                return;
            }

            // Update the path preview
            UpdatePathPreview();
        }

        /// <summary>
        /// Handles the "Create" button click event in the asset creation dialog;
        /// validates the input, collects form field values, constructs the full asset path,
        /// ensures the target folder exists, and invokes the creation callback with the collected data
        /// </summary>
        private void OnCreateClicked()
        {
            string fileName = _fileNameField.value?.Trim();

            // Exit case - no file name entered
            if (string.IsNullOrEmpty(fileName))
                return;

            // Collect field values
            Dictionary<string, string> fieldValues = new Dictionary<string, string>();
            fieldValues["fileName"] = fileName;

            foreach (KeyValuePair<string, TextField> kvp in _textFields)
            {
                fieldValues[kvp.Key] = kvp.Value.value?.Trim() ?? "";
            }

            // Build full path
            StringBuilder fullPathBuilder = new StringBuilder();
            fullPathBuilder.Append(_selectedFolder);
            fullPathBuilder.Append("/");
            fullPathBuilder.Append(fileName);
            fullPathBuilder.Append(".asset");
            string fullPath = fullPathBuilder.ToString();

            // Ensure folder exists
            EnsureFolderExists(_selectedFolder);

            // Callback
            _onCreate?.Invoke(fieldValues, fullPath);

            Close();
        }

        /// <summary>
        /// Ensures that the specified folder path exists within the Unity project;
        /// creates any missing folders in the hierarchy if they do not already exist
        /// </summary>
        /// <param name="folderPath">The full path to the folder to verify or create; each level in the hierarchy will be checked and created if necessary</param>
        private void EnsureFolderExists(string folderPath)
        {
            // Exit case - the folder already exists
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            
            // Split path and create each folder level
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            StringBuilder pathBuilder = new StringBuilder();
            
            for (int i = 1; i < parts.Length; i++)
            {
                // Build the path
                pathBuilder.Clear();
                pathBuilder.Append(currentPath);
                pathBuilder.Append("/");
                pathBuilder.Append(parts[i]);
                string nextPath = pathBuilder.ToString();

                // Skip if the folder already exists
                if (AssetDatabase.IsValidFolder(nextPath))
                {
                    currentPath = nextPath;
                    continue;
                }
                
                // Create the folder
                AssetDatabase.CreateFolder(currentPath, parts[i]);
                currentPath = nextPath;
            }
        }
    }
}