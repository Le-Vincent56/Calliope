using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
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
        protected List<TRowData> Rows = new List<TRowData>();
        protected ScrollView RowsContainer;
        protected Action OnRowsChanged;
        
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
            container.style.marginBottom = 4;
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            
            // Row number label
            Label indexLabel = new Label((index + 1).ToString());
            indexLabel.style.width = 24;
            indexLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            indexLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            
            container.Add(indexLabel);
            
            // Build specific fields for this tab
            BuildRowFields(container, Rows[index]);
            
            // Remove button
            int capturedIndex = index;
            Button removeButton = new Button(() => RemoveRow(capturedIndex));
            removeButton.text = "-";
            removeButton.style.width = 24;
            removeButton.style.marginLeft = 8;
            removeButton.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            container.Add(removeButton);
            
            return container;
        }

        /// <summary>
        /// Constructs and adds the necessary fields for a single row in the UI,
        /// using the data provided in the associated row data object
        /// </summary>
        /// <param name="container">The container where the row fields will be added</param>
        /// <param name="rowData">The data object that provides the information needed to populate the row fields</param>
        protected abstract void BuildRowFields(VisualElement container, TRowData rowData);

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
