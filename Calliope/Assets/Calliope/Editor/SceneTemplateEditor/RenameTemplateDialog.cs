using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// A dialog window used to rename a scene template within the Scene Template Editor;
    /// allows users to input a new name for the template and confirms the change through a callback action
    /// </summary>
    public class RenameTemplateDialog : EditorWindow
    {
        private string _newName;
        private Action<string> _onConfirm;

        /// <summary>
        /// Displays the RenameTemplateDialog window, allowing users to rename a template and handle the confirmation action
        /// </summary>
        /// <param name="currentName">The current name of the template being renamed, used to initialize the dialog input field</param>
        /// <param name="onConfirm">A callback action triggered when the user confirms the renaming, receiving the new name as a parameter</param>
        public static void Show(string currentName, Action<string> onConfirm)
        {
            RenameTemplateDialog window = GetWindow<RenameTemplateDialog>(true, "Rename Template", true);
            window._newName = currentName;
            window._onConfirm = onConfirm;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(400, 100);
            window.ShowUtility();
        }

        /// <summary>
        /// Constructs and populates the GUI for the RenameTemplateDialog window;
        /// this method initializes UI elements such as input fields and buttons, sets their properties,
        /// and defines their behaviors within the editor window
        /// </summary>
        private void CreateGUI()
        {
            // Create the root element
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            // Add the input field
            TextField nameField = new TextField("NewName");
            nameField.value = _newName;
            nameField.RegisterValueChangedCallback(evt => _newName = evt.newValue);
            root.Add(nameField);

            // Add the button section
            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.FlexEnd;
            buttonRow.style.marginTop = 10;

            // Add the cancel button
            Button cancelButton = new Button(() => Close());
            cancelButton.text = "Cancel";
            cancelButton.style.marginRight = 8;
            buttonRow.Add(cancelButton);

            // Add the confirm button
            Button confirmButton = new Button(() =>
            {
                _onConfirm?.Invoke(_newName);
                Close();
            });
            confirmButton.text = "Rename";
            buttonRow.Add(confirmButton);

            root.Add(buttonRow);

            // Focus the text field
            nameField.Focus();
        }
    }
}