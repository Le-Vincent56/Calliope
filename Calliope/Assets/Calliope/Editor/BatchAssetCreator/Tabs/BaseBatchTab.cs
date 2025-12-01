using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Editor.BatchAssetCreator.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents an abstract base class for creating and managing a tab interface
    /// for batch asset creation, allowing the organization of rows of data and
    /// providing features for UI interaction and asset generation
    /// </summary>
    /// <typeparam name="TRowData">The type of row data, which must inherit from BaseRowData</typeparam>
    public abstract class BaseBatchTab<TRowData> : IBatchTab where TRowData : BaseRowData, new()
    {
        private static readonly Color RowColorEven = new Color(0.22f, .22f, 0.22f);
        private static readonly Color RowColorOdd = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color SeparatorColor = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color HeaderColor = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color HeaderTextColor = new Color(0.8f, 0.8f, 0.8f);
        
        protected List<TRowData> Rows = new List<TRowData>();
        protected ScrollView RowsContainer;
        protected Action OnRowsChanged;

        private const float RowNumberWidth = 32f;
        private const float RemoveButtonWidth = 28f;
        private const float SeparatorWidth = 1f;
        private const float CellPadding = 8f;
        
        public int ValidRowCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Rows.Count; i++)
                {
                    if (Rows[i].IsValid) count++;
                }
                return count;
            }
        }
        
        public abstract string TabName { get; }
        protected abstract string SubfolderName { get; }
        protected abstract ColumnDefinition[] Columns { get; }

        /// <summary>
        /// Constructs the content for the tab, including a scrollable rows container
        /// and an "Add Row" button, and initializes the UI with one default row
        /// </summary>
        /// <param name="onRowsChanged">Callback triggered when the collection of rows changes</param>
        /// <returns>A visual element containing the constructed tab content</returns>
        public VisualElement BuildContent(Action onRowsChanged)
        {
            OnRowsChanged = onRowsChanged;

            VisualElement container = new VisualElement();
            container.style.flexGrow = 1;
            
            // Header row
            VisualElement headerRow = CreateHeaderRow();
            container.Add(headerRow);
            
            // Scrollable rows container
            RowsContainer = new ScrollView();
            RowsContainer.style.flexGrow = 1;
            container.Add(RowsContainer);
            
            // Creat the "Add Row" button
            Button addRowButton = new Button(AddRow);
            addRowButton.text = "+ Add Row";
            addRowButton.style.marginTop = 8;
            addRowButton.style.alignSelf = Align.FlexStart;
            container.Add(addRowButton);
            
            // Initialize with one empty row
            if(Rows.Count == 0) Rows.Add(new TRowData());

            RefreshRows();
            
            return container;
        }

        /// <summary>
        /// Rebuilds the visual representation of the rows by clearing the current container
        /// and recreating each row element based on the existing data in the list of rows
        /// </summary>
        public void RefreshRows()
        {
            RowsContainer.Clear();

            for (int i = 0; i < Rows.Count; i++)
            {
                RowsContainer.Add(CreateRowElement(i));
            }
        }

        /// <summary>
        /// Creates a visual element representing a single row in the UI, including
        /// a row number label, data fields, and a remove button, based on the provided index
        /// </summary>
        /// <param name="index">The zero-based index of the row to create, representing its position in the row list</param>
        /// <returns>A visual element containing the UI representation of the row</returns>
        protected VisualElement CreateRowElement(int index)
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingLeft = CellPadding;
            container.style.paddingRight = CellPadding;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.flexShrink = 0;
            
            // Alternating row colors
            container.style.backgroundColor = (index % 2 == 0) ? RowColorEven : RowColorOdd;
            
            // Row number label
            Label indexLabel = new Label((index + 1).ToString());
            indexLabel.style.width = RowNumberWidth;
            indexLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            indexLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            container.Add(indexLabel);
            
            // Add separator after index
            container.Add(CreateSeparator());
            
            // Build specific fields for this tab
            BuildRowFields(container, Rows[index], index);
            
            // Remove button
            int capturedIndex = index;
            Button removeButton = new Button(() => RemoveRow(capturedIndex));
            removeButton.text = "X";
            removeButton.style.width = RemoveButtonWidth;
            removeButton.style.height = 20;
            removeButton.style.marginLeft = CellPadding;
            removeButton.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            container.Add(removeButton);
            
            return container;
        }

        /// <summary>
        /// Creates a vertical separator element with a fixed width and height
        /// and a predefined color, which can be used to visually separate UI components
        /// </summary>
        /// <returns>A visual element styled as a separator</returns>
        protected VisualElement CreateSeparator()
        {
            VisualElement separator = new VisualElement();
            separator.style.width = SeparatorWidth;
            separator.style.height = 20;
            separator.style.backgroundColor = SeparatorColor;
            separator.style.marginLeft = CellPadding;
            separator.style.marginRight = CellPadding;
            return separator;
        }

        /// <summary>
        /// Creates the header row for the batch asset tab, including the row number
        /// label, column headers, and spacing for the remove button
        /// </summary>
        /// <returns>A visual element that represents the constructed header row</returns>
        private VisualElement CreateHeaderRow()
        {
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
            
            // Row number header
            Label rowHeader = new Label("Row");
            rowHeader.style.width = RowNumberWidth;
            rowHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
            rowHeader.style.color = HeaderTextColor;
            rowHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(rowHeader);

            header.Add(CreateSeparator());
            
            // Column headers
            ColumnDefinition[] columns = Columns;
            for (int i = 0; i < columns.Length; i++)
            {
                ColumnDefinition column = columns[i];
                
                // Create the label
                Label columnHeader = new Label(column.Header);
                columnHeader.style.color = HeaderTextColor;
                columnHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                columnHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
                columnHeader.style.paddingLeft = 4;

                // Use fixed width or flex grow to determine column width
                if (column.Width.HasValue) columnHeader.style.width = column.Width.Value;
                else columnHeader.style.flexGrow = column.FlexGrow > 0 ? column.FlexGrow : 1;
                
                // Add the tool tip
                if(!string.IsNullOrEmpty(column.Tooltip)) columnHeader.tooltip = column.Tooltip;
                
                header.Add(columnHeader);
                
                // Skip if the last column
                if(i >= columns.Length - 1) continue;
                
                // Add a separator between columns
                header.Add(CreateSeparator());
            }
            
            // Spacer for remove button column
            VisualElement removeHeaderSpacer = new VisualElement();
            removeHeaderSpacer.style.width = RemoveButtonWidth + CellPadding;
            header.Add(removeHeaderSpacer);
            
            return header;
        }

        /// <summary>
        /// Creates a visual cell element based on the column definition, sets its layout properties,
        /// and adds the specified content to it
        /// </summary>
        /// <param name="columnIndex">The index of the column for which the cell is being created</param>
        /// <param name="content">The visual content to be added to the cell</param>
        /// <returns>The constructed visual cell element with the specified content and layout</returns>
        protected VisualElement CreateCell(int columnIndex, VisualElement content)
        {
            ColumnDefinition column = Columns[columnIndex];

            // Create the cell
            VisualElement cell = new VisualElement();
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;
            
            // Set the cell width based on the column definition
            if(column.Width.HasValue) cell.style.width = column.Width.Value;
            else cell.style.flexGrow = column.FlexGrow > 0 ? column.FlexGrow : 1;
            
            // Add the content to the cell
            content.style.flexGrow = 1;
            cell.Add(content);

            return cell;
        }

        /// <summary>
        /// Populates the provided container with fields representing the specified row data,
        /// allowing customization and user interaction for the row at the given index
        /// </summary>
        /// <param name="container">The container to which the row fields are added</param>
        /// <param name="rowData">The data object representing the row to be displayed and edited</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected abstract void BuildRowFields(VisualElement container, TRowData rowData, int rowIndex);

        /// <summary>
        /// Creates assets based on the data stored in the rows of the batch tab,
        /// and outputs them to the specified base folder path; the implementation
        /// of the method is specific to the derived class and the type of data
        /// being processed
        /// </summary>
        /// <param name="baseFolderPath">The base folder path where assets will be created</param>
        /// <returns>Returns the number of assets successfully created</returns>
        public abstract int CreateAssets(string baseFolderPath);

        /// <summary>
        /// Adds a new row to the collection, initializes it with default data,
        /// and updates the UI to reflect the changes
        /// </summary>
        protected void AddRow()
        {
            Rows.Add(new TRowData());
            RefreshRows();
            OnRowsChanged?.Invoke();
        }

        /// <summary>
        /// Removes a row from the collection of rows at the specified index and updates the UI;
        /// ensures that at least one row is always retained
        /// </summary>
        /// <param name="index">The zero-based index of the row to be removed</param>
        protected void RemoveRow(int index)
        {
            // Exit case - cannot remove the last row
            if (Rows.Count <= 1) return; 
            
            Rows.RemoveAt(index);
            RefreshRows();
            OnRowsChanged?.Invoke();
        }

        /// <summary>
        /// Clears the list of rows, resets it with a single empty row, and refreshes the UI to reflect the updated list;
        /// invokes the rows changed callback to notify about the updates
        /// </summary>
        public void ClearRows()
        {
            Rows.Clear();
            Rows.Add(new TRowData());
            RefreshRows();
            OnRowsChanged?.Invoke();
        }

        /// <summary>
        /// Performs validation on the collection of rows in the tab, ensuring they meet the defined constraints
        /// and detecting potential issues like duplicate identifiers or missing required fields
        /// </summary>
        /// <param name="baseFolderPath">The base folder path used to resolve file paths or validate relative file references within the rows</param>
        /// <returns>A ValidationResult object containing the validation results and any associated error or warning messages</returns>
        public virtual ValidationResult Validate(string baseFolderPath)
        {
            ValidationResult results = new ValidationResult();
            HashSet<string> seenIDs = new HashSet<string>();

            StringBuilder messageBuilder = new StringBuilder();
            
            for (int i = 0; i < Rows.Count; i++)
            {
                TRowData row = Rows[i];
                
                // Skip completely empty rows
                if (!row.HasAnyData) continue;
                
                // Skip if the row is not valid
                if (!row.IsValid)
                {
                    // Build the message
                    messageBuilder.Clear();
                    messageBuilder.Append("Row ");
                    messageBuilder.Append(i + 1);
                    messageBuilder.Append(" is incomplete: missing required fields");
                    
                    // Add the error
                    results.AddError(messageBuilder.ToString(), i);
                    continue;
                }

                // Get the Row ID
                string rowID = GetRowID(row);

                // Skip if the ID is empty
                if (string.IsNullOrEmpty(rowID)) continue;
                
                // Skip if adding the Row ID was successful (no duplicates)
                if (seenIDs.Add(rowID)) continue;
                
                // Build the message
                messageBuilder.Clear();
                messageBuilder.Append("Row ");
                messageBuilder.Append(i + 1);
                messageBuilder.Append(" has duplicate ID '");
                messageBuilder.Append(rowID);
                messageBuilder.Append("'");
                        
                // Add the error
                results.AddError(messageBuilder.ToString(), i);
            }

            return results;
        }

        /// <summary>
        /// Retrieves a unique identifier for the specified row of data; the implementation of this method
        /// should define how the row's ID is determined based on its content or other properties
        /// </summary>
        /// <param name="row">The row data from which the identifier will be extracted or derived</param>
        /// <returns>A string representing the unique identifier for the specified row</returns>
        protected abstract string GetRowID(TRowData row);

        /// <summary>
        /// Ensures that a specific subfolder exists within the provided base folder path;
        /// if the subfolder does not exist, it will be created
        /// </summary>
        /// <param name="baseFolderPath">The path of the base folder where the subfolder should exist</param>
        /// <returns>The full path of the subfolder within the base folder</returns>
        protected string EnsureSubfolder(string baseFolderPath)
        {
            // Create the path string
            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(baseFolderPath);
            pathBuilder.Append("/");
            pathBuilder.Append(SubfolderName);
            string fullPath = pathBuilder.ToString();
            
            // Create the folder if it doesn't exist
            if(!AssetDatabase.IsValidFolder(fullPath)) 
                AssetDatabase.CreateFolder(baseFolderPath, SubfolderName);

            return fullPath;
        }

        /// <summary>
        /// Parses a comma-separated string into an array of trimmed strings
        /// </summary>
        /// <param name="input">The input string containing comma-separated values</param>
        /// <returns>An array of strings where each element represents a trimmed value from the input</returns>
        protected string[] ParseCommaSeparated(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();

            // Separate the parts by commas
            string[] parts = input.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return parts;
        }
    }
}
