using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a batch tab for managing and editing Variation Set data in the Batch Asset Creator tool;
    /// inherits functionality to handle operations related to VariationSetRowData objects
    /// </summary>
    public class VariationSetBatchTab : BaseBatchTab<VariationSetRowData>
    {
        public override string TabName => "Variation Sets";
        protected override string SubfolderName => "Variation Sets";

        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("ID", 120),
            new ColumnDefinition("Display Name", 180),
            new ColumnDefinition("Description", flexGrow: 1)
        };
        
        protected override string AssetTypeName => "VariationSetSO";
        
        /// <summary>
        /// Configures the row fields in the given container based on the provided VariationSetRowData instance
        /// </summary>
        /// <param name="container">The visual element container where the row fields will be added</param>
        /// <param name="data">The data object containing the values to populate the fields</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected override void BuildRowFields(VisualElement container, VariationSetRowData data, int rowIndex)
        {
            // ID field
            TextField idField = new TextField();
            idField.value = data.ID;
            idField.RegisterValueChangedCallback(evt => data.ID = evt.newValue);
            container.Add(CreateCell(0, idField));
            
            container.Add(CreateSeparator());
            
            // Display name field
            TextField displayNameField = new TextField();
            displayNameField.value = data.DisplayName;
            displayNameField.RegisterValueChangedCallback(evt => data.DisplayName = evt.newValue);
            container.Add(CreateCell(1, displayNameField));
            
            // Description field
            TextField descriptionField = new TextField();
            descriptionField.value = data.Description;
            descriptionField.RegisterValueChangedCallback(evt => data.Description = evt.newValue);
            container.Add(CreateCell(2, descriptionField));
        }

        /// <summary>
        /// Creates assets within the specified base folder, ensuring the folder structure is in place
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder where assets will be created</param>
        /// <returns>The number of assets successfully created</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();
            
            for (int i = 0; i < Rows.Count; i++)
            {
                VariationSetRowData data = Rows[i];

                // Exit case - skip invalid rows
                if (!data.IsValid) continue;

                // Create the asset
                VariationSetSO asset = ScriptableObject.CreateInstance<VariationSetSO>();
                
                // Set values
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("id").stringValue = data.ID;
                serialized.FindProperty("displayName").stringValue = data.DisplayName;
                serialized.FindProperty("description").stringValue = data.Description;
                serialized.ApplyModifiedProperties();
                
                // Create the file name
                fileBuilder.Clear();
                if (string.IsNullOrEmpty(data.ID))
                {
                    fileBuilder.Append("VariationSet_");
                    fileBuilder.Append(i);
                }
                else
                {
                    fileBuilder.Append("VariationSet_");
                    fileBuilder.Append(data.ID);
                }
                string fileName = fileBuilder.ToString();
                
                // Build the path string
                fileBuilder.Clear();
                fileBuilder.Append(subfolder);
                fileBuilder.Append("/");
                fileBuilder.Append(fileName);
                fileBuilder.Append(".asset");
                string assetPath = fileBuilder.ToString();
                AssetDatabase.CreateAsset(asset, assetPath);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Retrieves the unique identifier (ID) for the specified VariationSetRowData object
        /// </summary>
        /// <param name="data">The VariationSetRowData object containing the ID to retrieve</param>
        /// <returns>The unique identifier (ID) of the provided VariationSetRowData</returns>
        protected override string GetRowID(VariationSetRowData data) => data.ID;

        /// <summary>
        /// Sets the ID of a specific row in the data set to a new value
        /// </summary>
        /// <param name="row">The row data object whose ID is to be updated</param>
        /// <param name="newID">The new ID value to assign to the row</param>
        protected override void SetRowID(VariationSetRowData row, string newID) => row.ID = newID;
    }
}