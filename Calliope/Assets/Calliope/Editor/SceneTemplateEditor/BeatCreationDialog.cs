using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Represents a dialog window for creating a new "beat" within the Unity Editor;
    /// Provides functionality to input a beat ID and potentially additional metadata
    /// for creating a new beat; the dialog validates the input and communicates the
    /// creation data via callback functions
    /// </summary>
    public class BeatCreationDialog : EditorWindow
    {
        private TextField _beatIDField;
        private TextField _displayNameField;
        private Button _createButton;
        private Button _cancelButton;
        private Label _errorLabel;
        
        private Action<string, string> _onCreate;
        private Func<string, bool> _onValidateID;

        /// <summary>
        /// Displays the BeatCreationDialog window for creating a new beat
        /// </summary>
        /// <param name="onCreate">
        /// A callback function that is invoked when the user confirms the creation
        /// of a new beat; the first string parameter represents the beat ID, and the
        /// second string parameter represents additional data associated with the beat
        /// </param>
        /// <param name="onValidateID">
        /// A callback function that is used to validate the beat ID entered by the user;
        /// it takes a string representing the beat ID and returns a boolean indicating
        /// whether the ID is valid or not
        /// </param>
        public static void Show(Action<string, string> onCreate, Func<string, bool> onValidateID)
        {
            BeatCreationDialog window = GetWindow<BeatCreationDialog>(
                true,
                "Create New Beat",
                true
            );
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);
            window._onCreate = onCreate;
            window._onValidateID = onValidateID;
        }

        private void CreateGUI()
        {
            // Root container
            VisualElement root = rootVisualElement;
            root.style.paddingTop = new StyleLength(16);
            root.style.paddingLeft = new StyleLength(16);
            root.style.paddingBottom = new StyleLength(16);
            root.style.paddingRight = new StyleLength(16);
            
            // Title
            Label titleLabel = new Label("Create New Beat");
            titleLabel.style.fontSize = 16;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 12;
            root.Add(titleLabel);
            
            // Beat ID field
            _beatIDField = new TextField("Beat ID");
            _beatIDField.style.marginBottom = 12;
            _beatIDField.RegisterValueChangedCallback(evt => OnBeatIDChanged());
            root.Add(_beatIDField);
            
            // Display name field
            _displayNameField = new TextField("Display Name (Optional)");
            _displayNameField.style.marginBottom = 12;
            root.Add(_displayNameField);
            
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
            
            // Cancel button
            _cancelButton = new Button(OnCancel);
            _cancelButton.text = "Cancel";
            _cancelButton.style.marginRight = 8;
            _cancelButton.style.minWidth = 80;
            buttonContainer.Add(_cancelButton);
            
            // Create button
            _createButton = new Button(OnCreate);
            _createButton.text = "Create";
            _createButton.style.minWidth = 80;
            _createButton.SetEnabled(false);
            buttonContainer.Add(_createButton);
            
            root.Add(buttonContainer);
            
            // Focus the beat ID field by default
            _beatIDField.schedule.Execute(() => _beatIDField.Focus());
        }

        /// <summary>
        /// Handles changes to the Beat ID input field, enabling or disabling the "Create" button
        /// and displaying error messages based on the validity of the entered Beat ID
        /// </summary>
        private void OnBeatIDChanged()
        {
            string beatID = _beatIDField.value?.Trim();
            
            // Exit case - if the ID is empty
            if (string.IsNullOrWhiteSpace(beatID))
            {
                _createButton.SetEnabled(false);
                _errorLabel.style.display = DisplayStyle.None;
                return;
            }
            
            // Validate the ID
            if (_onValidateID != null && !_onValidateID(beatID))
            {
                _createButton.SetEnabled(false);
                
                StringBuilder errorBuilder = new StringBuilder();
                errorBuilder.Append("Beat ID '");
                errorBuilder.Append(beatID);
                errorBuilder.Append("' already exists");
                _errorLabel.text = errorBuilder.ToString();
                _errorLabel.style.display = DisplayStyle.Flex;
                
                return;
            }
            
            // The ID is valid, so allow the create button
            _createButton.SetEnabled(true);
            _errorLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Handles the creation of a new beat by invoking the assigned callback with
        /// the provided beat ID and optional display name; closes the dialog upon successful invocation
        /// </summary>
        private void OnCreate()
        {
            string beatID = _beatIDField.value?.Trim();
            string displayName = _displayNameField.value?.Trim();

            // Exit case - no beat ID entered
            if (string.IsNullOrEmpty(beatID)) return;
            
            // Invoke the callback and close the window
            _onCreate?.Invoke(beatID, displayName);
            Close();
        }

        /// <summary>
        /// Closes the BeatCreationDialog window when the "Cancel" button is clicked
        /// </summary>
        private void OnCancel() => Close();
    }
}
