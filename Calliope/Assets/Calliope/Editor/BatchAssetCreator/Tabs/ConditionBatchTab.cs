using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Editor.BatchAssetCreator.ConditionBuilders;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a batch tab specifically used for managing and creating
    /// condition-based assets within the batch asset creation process of the editor
    /// </summary>
    public class ConditionBatchTab : IBatchTab
    {
        private static readonly Color SubtabActiveColor = new Color(0.3f, 0.5f, 0.7f);
        private static readonly Color SubtabInactiveColor = new Color(0.25f, 0.25f, 0.25f);
        
        private List<ConditionSubtab> _subtabs;
        private List<Button> _subtabButtons;
        private int _currentSubtabIndex = 0;
        private VisualElement _subtabBar;
        private VisualElement _subtabContent;
        private Action _onRowsChanged;

        public string TabName => "Conditions";

        public int ValidRowCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _subtabs.Count; i++)
                {
                    count += _subtabs[i].ValidRowCount;
                }
                return count;
            }
        }

        /// <summary>
        /// Builds and returns the content structure for the condition batch tab,
        /// including a subtab bar and a content area for displaying subtab-specific data
        /// </summary>
        /// <param name="onRowsChanged">Callback invoked when rows are modified within the tab</param>
        /// <returns>A VisualElement representing the condition batch tab's content layout</returns>
        public VisualElement BuildContent(Action onRowsChanged)
        {
            _onRowsChanged = onRowsChanged;

            // Initialize subtabs from registry (if not created already)
            if (_subtabs == null)
            {
                IReadOnlyList<IConditionRowBuilder> builders = ConditionBuilderRegistry.Builders;
                _subtabs = new List<ConditionSubtab>(builders.Count);
                for (int i = 0; i < builders.Count; i++)
                {
                    _subtabs.Add(new ConditionSubtab(builders[i]));
                }
            }

            // Create the container
            VisualElement container = new VisualElement();
            container.style.flexGrow = 1;

            // Subtab bar
            _subtabBar = CreateSubtabBar();
            container.Add(_subtabBar);
            
            // Subtab content area
            _subtabContent = new VisualElement();
            _subtabContent.style.flexGrow = 1;
            _subtabContent.style.marginTop = 8;
            container.Add(_subtabContent);
            
            // Show first subtab
            ShowSubtab(_currentSubtabIndex);

            return container;
        }

        /// <summary>
        /// Creates a subtab bar containing clickable buttons for navigating between subtabs
        /// </summary>
        /// <returns>A VisualElement representing the subtab bar with buttons for each subtab</returns>
        private VisualElement CreateSubtabBar()
        {
            // Set up the bar element
            VisualElement bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.marginBottom = 4;
            
            // Create the list of buttons
            _subtabButtons = new List<Button>(_subtabs.Count);

            for (int i = 0; i < _subtabs.Count; i++)
            {
                int capturedIndex = i;
                ConditionSubtab subtab = _subtabs[i];

                // Create the button
                Button button = new Button(() => ShowSubtab(capturedIndex));
                button.text = subtab.Builder.DisplayName;
                button.style.marginRight = 4;
                button.style.paddingLeft = 12;
                button.style.paddingRight = 12;
                button.style.paddingTop = 6;
                button.style.paddingBottom = 6;
                
                _subtabButtons.Add(button);
                bar.Add(button);
            }

            return bar;
        }

        /// <summary>
        /// Displays the subtab at the specified index, updating the UI to reflect the currently active subtab
        /// </summary>
        /// <param name="index">The index of the subtab to display. Must be a valid index within the list of subtabs</param>
        private void ShowSubtab(int index)
        {
            _currentSubtabIndex = index;
            
            // Update button styling
            for (int i = 0; i < _subtabButtons.Count; i++)
            {
                _subtabButtons[i].style.backgroundColor = i == index 
                    ? SubtabActiveColor
                    : SubtabInactiveColor;
            }
            
            // Rebuild content
            _subtabContent.Clear();
            _subtabContent.Add(_subtabs[index].BuildContent(_onRowsChanged));
            
            // Reapply validation highlighting if validation was previously run
            ValidationResult subtabResult = _subtabs[index].Validate();
            _subtabs[index].UpdateRowHighlighting(subtabResult);
        }

        /// <summary>
        /// Clears all rows from the currently active subtab, resetting its contents entirely
        /// </summary>
        public void ClearRows() => _subtabs[_currentSubtabIndex].ClearRows();

        /// <summary>
        /// Removes all empty rows from the currently active subtab, ensuring only valid rows remain
        /// </summary>
        public void ClearEmptyRows() => _subtabs[_currentSubtabIndex].ClearEmptyRows();

        /// <summary>
        /// Creates assets for all subtabs by iterating through them and using their individual
        /// CreateAssets implementations; the assets are created in a specified subfolder of the base folder
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder where assets will be created</param>
        /// <returns>The total number of assets created across all subtabs</returns>
        public int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            
            // Create assets from all subtabs
            for (int i = 0; i < _subtabs.Count; i++)
            {
                count += _subtabs[i].CreateAssets(subfolder);
            }
            
            return count;
        }

        /// <summary>
        /// Validates the specified base folder path by performing checks across all subtabs
        /// and aggregates the validation results, including any error messages or warnings
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder to be validated</param>
        /// <returns>A ValidationResult containing the aggregated results and messages from all subtabs</returns>
        public ValidationResult Validate(string baseFolderPath)
        {
            ValidationResult result = new ValidationResult();
            StringBuilder messageBuilder = new StringBuilder();
            
            // Validate all subtabs
            for(int i = 0; i < _subtabs.Count; i++)
            {
                ValidationResult subtabResult = _subtabs[i].Validate();
                
                // Prefix errors with subtab name
                foreach (ValidationMessage message in subtabResult.Messages)
                {
                    messageBuilder.Clear();
                    
                    // Add prefix
                    messageBuilder.Append("[");
                    messageBuilder.Append(_subtabs[i].Builder.DisplayName);
                    messageBuilder.Append("] ");
                    messageBuilder.Append(message.Message);

                    // Add to result
                    switch (message.Severity)
                    {
                        case ValidationSeverity.Error:
                            result.AddError(messageBuilder.ToString(), message.RowIndex);
                            break;
                        
                        case ValidationSeverity.Warning:
                            result.AddWarning(messageBuilder.ToString(), message.RowIndex);
                            break;
                        
                        default:
                            result.AddInfo(messageBuilder.ToString(), message.RowIndex);
                            break;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Updates the row highlighting for all subtabs within the condition batch tab based on validation results
        /// </summary>
        /// <param name="result">The validation result to be used for determining row highlighting</param>
        public void UpdateRowHighlighting(ValidationResult result)
        {
            for (int i = 0; i < _subtabs.Count; i++)
            {
                ValidationResult subtabResult = _subtabs[i].Validate();
                _subtabs[i].UpdateRowHighlighting(subtabResult);
            }
        }

        /// <summary>
        /// Clears all highlighting applied to rows across the subtabs in the condition batch tab;
        /// this is typically used to reset visual state after validation errors or user interaction
        /// </summary>
        public void ClearRowHighlighting()
        {
            for(int i = 0; i < _subtabs.Count; i++)
            {
                _subtabs[i].ClearRowHighlighting();
            }
        }

        /// <summary>
        /// Ensures that a subfolder named "Conditions" exists within the given base folder path;
        /// if the "Conditions" folder does not exist, it is created
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder where the "Conditions" subfolder should exist</param>
        /// <returns>The full path to the "Conditions" subfolder</returns>
        private string EnsureSubfolder(string baseFolderPath)
        {
            // Create the full path
            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(baseFolderPath);
            pathBuilder.Append("/");
            pathBuilder.Append("Conditions");
            string fullPath = pathBuilder.ToString();
            
            // Create the folder if it doesn't exist
            if(!AssetDatabase.IsValidFolder(fullPath)) 
                AssetDatabase.CreateFolder(baseFolderPath, "Conditions");
            
            return fullPath;
        }
    }
}