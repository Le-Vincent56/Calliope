using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Editor.BatchAssetCreator.ConditionBuilders;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Validation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a subtab used within a batch asset creation process to manage and configure
    /// condition-based settings and rows dynamically using a specified condition row builder
    /// </summary>
    public class ConditionSubtab
    {
        private static readonly Color RowColorEven = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color RowColorOdd = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color SeparatorColor = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color HeaderColor = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color HeaderTextColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color InvalidRowBorderColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color WarningRowBorderColor = new Color(0.8f, 0.6f, 0.2f);
        
        private const float RowNumberWidth = 32f;
        private const float ActionButtonWidth = 28f;
        private const float CellPadding = 8f;
        
        private readonly IConditionRowBuilder _builder;
        private List<BaseConditionRowData> _rows = new List<BaseConditionRowData>();
        private ScrollView _rowsContainer;
        private Action _onRowsChanged;
        private Dictionary<int, VisualElement> _rowElements = new Dictionary<int, VisualElement>();
        private HashSet<int> _invalidRowIndices = new HashSet<int>();
        private HashSet<int> _warningRowIndices = new HashSet<int>();
        
        public IConditionRowBuilder Builder => _builder;

        public int ValidRowCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].IsValid) count++;
                }
                return count;
            }
        }
        
        public int TotalRowCount => _rows.Count;
        
        public ConditionSubtab(IConditionRowBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Builds and returns a VisualElement representing the content of the condition subtab
        /// </summary>
        /// <param name="onRowsChanged">
        /// Callback invoked whenever the rows in the subtab are modified
        /// </param>
        /// <returns>
        /// A VisualElement containing the UI elements for the condition subtab, including the header, rows container, and add row button
        /// </returns>
        public VisualElement BuildContent(Action onRowsChanged)
        {
            _onRowsChanged = onRowsChanged;

            VisualElement container = new VisualElement();
            container.style.flexGrow = 1;
            
            // Header row
            container.Add(CreateHeaderRow());

            // Scrollable rows
            _rowsContainer = new ScrollView();
            _rowsContainer.style.flexGrow = 1;
            container.Add(_rowsContainer);

            // Add Row button
            Button addRowButton = new Button(AddRow);
            addRowButton.text = "+ Add Row";
            addRowButton.style.marginTop = 8;
            addRowButton.style.alignSelf = Align.FlexStart;
            container.Add(addRowButton);

            // Initialize with one empty row if needed
            if (_rows.Count == 0)
            {
                _rows.Add(_builder.CreateRowData());
            }

            RefreshRows();
            return container;
        }

        /// <summary>
        /// Creates and returns a VisualElement representing the header row for the subtab,
        /// including the row number, column labels, and action spacers
        /// </summary>
        /// <returns>
        /// A VisualElement configured as a styled header row containing labels for columns
        /// and associated separators
        /// </returns>
        private VisualElement CreateHeaderRow()
        {
            ColumnDefinition[] columns = _builder.Columns;

            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor = HeaderColor;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 6;
            header.style.paddingLeft = CellPadding;
            header.style.paddingRight = CellPadding;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = SeparatorColor;

            // Row number
            Label rowHeader = new Label("Row");
            rowHeader.style.width = RowNumberWidth;
            rowHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
            rowHeader.style.color = HeaderTextColor;
            rowHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(rowHeader);

            header.Add(CreateSeparator());

            // Columns from builder
            for (int i = 0; i < columns.Length; i++)
            {
                ColumnDefinition column = columns[i];
                
                // Create cell container to match row-cell widths
                VisualElement cell = new VisualElement();
                cell.style.flexDirection = FlexDirection.Row;
                cell.style.alignItems = Align.Center;

                // Decide the column width
                if (column.Width.HasValue)
                {
                    cell.style.width = column.Width.Value;
                }
                else
                {
                    cell.style.flexGrow = column.FlexGrow > 0 
                        ? column.FlexGrow 
                        : 1;
                }

                Label columnHeader = new Label(column.Header);
                columnHeader.style.color = HeaderTextColor;
                columnHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                columnHeader.style.flexGrow = 1;

                // Add tooltip if needed
                if (!string.IsNullOrEmpty(column.Tooltip))
                    columnHeader.tooltip = column.Tooltip;

                cell.Add(columnHeader);
                header.Add(cell);

                // Skip if the last column
                if (i >= columns.Length - 1) continue;
                
                // Create a separator between columns
                header.Add(CreateSeparator());
            }

            // Actions spacer
            VisualElement actionsSpacer = new VisualElement();
            actionsSpacer.style.width = (ActionButtonWidth * 2) + CellPadding + 4;
            header.Add(actionsSpacer);

            return header;
        }

        /// <summary>
        /// Refreshes the rows displayed in the row container by clearing existing elements and re-adding
        /// UI elements for all rows in the subtab
        /// </summary>
        private void RefreshRows()
        {
            _rowsContainer.Clear();
            _rowElements.Clear();
            
            for (int i = 0; i < _rows.Count; i++)
            {
                VisualElement rowElement = CreateRowElement(i);
                
                // Set the row element with validation style
                _rowElements[i] = rowElement;
                ApplyRowValidationStyle(rowElement, i);
                _rowsContainer.Add(rowElement);
            }
        }

        /// <summary>
        /// Applies the appropriate validation style to a row container based on its validation state;
        /// this includes setting a visual border on the row to indicate if it is invalid, has warnings, or is valid
        /// </summary>
        /// <param name="container">
        /// The row container to which the validation style will be applied
        /// </param>
        /// <param name="index">
        /// The index of the row being validated within the rows collection
        /// </param>
        private void ApplyRowValidationStyle(VisualElement container, int index)
        {
            if (_invalidRowIndices.Contains(index))
            {
                container.style.borderLeftWidth = 3;
                container.style.borderLeftColor = InvalidRowBorderColor;
            }
            else if (_warningRowIndices.Contains(index))
            {
                container.style.borderLeftWidth = 3;
                container.style.borderLeftColor = WarningRowBorderColor;
            }
            else
            {
                container.style.borderLeftWidth = 0;
            }
        }

        /// <summary>
        /// Updates the visual highlighting of rows based on the provided validation result,
        /// applying styles to indicate errors and warnings
        /// </summary>
        /// <param name="result">
        /// The validation result containing messages that specify errors or warnings for specific rows
        /// </param>
        public void UpdateRowHighlighting(ValidationResult result)
        {
            // Clear the row highlights
            _invalidRowIndices.Clear();
            _warningRowIndices.Clear();

            // Exit case - no validation result
            if (result == null) return;

            foreach (ValidationMessage message in result.Messages)
            {
                // Skip if the row index is invalid
                if (message.RowIndex < 0) continue;

                switch (message.Severity)
                {
                    case ValidationSeverity.Error:
                        _invalidRowIndices.Add(message.RowIndex);
                        break;
                    
                    case ValidationSeverity.Warning:
                        // Skip if the row is already marked as invalid
                        if (_invalidRowIndices.Contains(message.RowIndex)) break;
                        
                        _warningRowIndices.Add(message.RowIndex);

                        break;
                }
            }

            // Update row borders
            foreach (KeyValuePair<int, VisualElement> kvp in _rowElements)
            {
                ApplyRowValidationStyle(kvp.Value, kvp.Key);
            }
        }

        /// <summary>
        /// Clears all row highlighting by resetting the tracked invalid and warning row indices,
        /// and removing any visual styling cues such as border highlights from the associated UI elements
        /// </summary>
        public void ClearRowHighlighting()
        {
            // Clear the row indices
            _invalidRowIndices.Clear();
            _warningRowIndices.Clear();
            
            // Reset row borders
            foreach (KeyValuePair<int, VisualElement> kvp in _rowElements)
            {
                kvp.Value.style.borderLeftWidth = 0;
            }
        }

        /// <summary>
        /// Creates and returns a VisualElement representing a row with specific properties and interactivity
        /// </summary>
        /// <param name="index">
        /// The index of the row to be created; used to retrieve row data and determine row appearance
        /// </param>
        /// <returns>
        /// A VisualElement that contains row fields, a duplicate button, a remove button, and styling based on the row's index
        /// </returns>
        private VisualElement CreateRowElement(int index)
        {
            BaseConditionRowData data = _rows[index];

            // Create the container
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingLeft = CellPadding;
            container.style.paddingRight = CellPadding;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.backgroundColor = (index % 2 == 0) 
                ? RowColorEven 
                : RowColorOdd;

            // Row number
            Label indexLabel = new Label((index + 1).ToString());
            indexLabel.style.width = RowNumberWidth;
            indexLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            indexLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            container.Add(indexLabel);

            container.Add(CreateSeparator());

            // Fields from builder
            _builder.BuildRowFields(container, data, () => _onRowsChanged?.Invoke());

            // Duplicate button
            int capturedIndex = index;
            Button duplicateButton = new Button(() => DuplicateRow(capturedIndex));
            duplicateButton.text = "D";
            duplicateButton.tooltip = "Duplicate this row";
            duplicateButton.style.width = ActionButtonWidth;
            duplicateButton.style.height = 20;
            duplicateButton.style.marginLeft = CellPadding;
            duplicateButton.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
            container.Add(duplicateButton);

            // Remove button
            Button removeButton = new Button(() => RemoveRow(capturedIndex));
            removeButton.text = "X";
            removeButton.tooltip = "Remove this row";
            removeButton.style.width = ActionButtonWidth;
            removeButton.style.height = 20;
            removeButton.style.marginLeft = 4;
            removeButton.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            container.Add(removeButton);

            return container;
        }

        /// <summary>
        /// Creates and returns a VisualElement representing a separator line used to divide elements in the UI layout
        /// </summary>
        /// <returns>
        /// A VisualElement styled as a vertical separator with defined width, height, background color, and margins
        /// </returns>
        private VisualElement CreateSeparator()
        {
            VisualElement separator = new VisualElement();
            separator.style.width = 1;
            separator.style.height = 20;
            separator.style.backgroundColor = SeparatorColor;
            separator.style.marginLeft = CellPadding;
            separator.style.marginRight = CellPadding;
            
            return separator;
        }

        /// <summary>
        /// Adds a new row to the subtab by creating a condition row data instance using the builder,
        /// updates the UI elements to reflect the new row, and invokes the rows changed callback
        /// </summary>
        private void AddRow()
        {
            _rows.Add(_builder.CreateRowData());
            RefreshRows();
            _onRowsChanged?.Invoke();
        }

        /// <summary>
        /// Removes a row from the condition subtab at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the row to remove</param>
        private void RemoveRow(int index)
        {
            // Exit case - always keep one row
            if (_rows.Count <= 1) return;
            
            _rows.RemoveAt(index);
            RefreshRows();
            _onRowsChanged?.Invoke();
        }

        /// <summary>
        /// Duplicates the row at the specified index by creating a clone of the row's data
        /// and inserting it directly below the original row
        /// </summary>
        /// <param name="index">The zero-based index of the row to duplicate</param>
        private void DuplicateRow(int index)
        {
            // Copy the data
            BaseConditionRowData clone = _rows[index].Clone();
            
            _rows.Insert(index + 1, clone);
            RefreshRows();
            _onRowsChanged?.Invoke();
        }

        /// <summary>
        /// Clears all rows in the condition subtab, initializes it with a default row created by the builder,
        /// and refreshes the displayed rows; invokes the callback to notify that rows have been changed
        /// </summary>
        public void ClearRows()
        {
            _rows.Clear();
            _rows.Add(_builder.CreateRowData());
            RefreshRows();
            _onRowsChanged?.Invoke();
        }

        /// <summary>
        /// Removes all empty rows from the condition subtab while ensuring at least one row remains;
        /// a row is considered empty if its data does not contain any meaningful content
        /// </summary>
        public void ClearEmptyRows()
        {
            for (int i = _rows.Count - 1; i >= 0; i--)
            {
                // Skip if the row is not empty
                if (_rows[i].HasAnyData) continue;
                
                _rows.RemoveAt(i);
            }
            
            // Ensure at least one row
            if (_rows.Count == 0)
                _rows.Add(_builder.CreateRowData());
            
            RefreshRows();
            _onRowsChanged?.Invoke();
        }

        /// <summary>
        /// Creates assets for all valid rows and saves them to the specified folder path
        /// </summary>
        /// <param name="folderPath">The path where the created assets will be stored</param>
        /// <returns>The number of successfully created assets</returns>
        public int CreateAssets(string folderPath)
        {
            int count = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                // Skip if the row is invalid
                if (!_rows[i].IsValid) continue;

                // Skip if the asset could not be created
                if (!_builder.CreateAsset(_rows[i], folderPath)) continue;
                
                count++;
            }
            return count;
        }

        /// <summary>
        /// Validates the condition rows within the subtab, checking for completeness and duplicate row IDs
        /// </summary>
        /// <returns>A ValidationResult object containing any errors detected during validation, such as incomplete or duplicate rows</returns>
        public ValidationResult Validate()
        {
            ValidationResult result = new ValidationResult();
            HashSet<string> seenIds = new HashSet<string>();

            for (int i = 0; i < _rows.Count; i++)
            {
                BaseConditionRowData row = _rows[i];

                // Skip if the row does not have any data
                if (!row.HasAnyData) continue;
                
                StringBuilder messageBuilder = new StringBuilder();

                if (!row.IsValid)
                {
                    messageBuilder.Clear();
                    messageBuilder.Append("Row ");
                    messageBuilder.Append(i + 1);
                    messageBuilder.Append("is incomplete");
                    
                    // Add the error
                    result.AddError(messageBuilder.ToString());
                    continue;
                }

                // Get the Row ID
                string rowID = row.GetRowID();

                // Skip if the ID is empty
                if (seenIds.Add(rowID)) continue;
                
                messageBuilder.Clear();
                messageBuilder.Append("Row ");
                messageBuilder.Append(i + 1);
                messageBuilder.Append("is a duplicate");
                    
                result.AddError(messageBuilder.ToString());
            }

            return result;
        }
    }
}