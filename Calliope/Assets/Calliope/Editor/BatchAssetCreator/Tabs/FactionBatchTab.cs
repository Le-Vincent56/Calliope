using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a tab in the batch asset creation editor for managing and creating faction assets
    /// </summary>
    /// <typeparam name="FactionRowData">
    /// The type of data managed within the tab; in this case, it corresponds to faction-specific rows derived from the <see cref="BaseRowData"/> class
    /// </typeparam>
    public class FactionBatchTab : BaseBatchTab<FactionRowData>
    {
        public override string TabName => "Factions";
        protected override string SubfolderName => "Factions";
        protected override string AssetTypeName => "FactionSO";
        
        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("ID", 120, tooltip: "Unique identifier (e.g., sealed_circle)"),
            new ColumnDefinition("Display Name", 150, tooltip: "Human-readable name (e.g., The Sealed Circle)"),
            new ColumnDefinition("Description", flexGrow: 1, tooltip: "What this faction believes and fights for")
        };

        /// <summary>
        /// Populates a container with editable UI fields representing the properties of a single data row
        /// </summary>
        /// <param name="container">The UI container where the row fields will be added</param>
        /// <param name="data">The data object representing the row, containing the values to display and edit</param>
        /// <param name="rowIndex">The index of the row being processed</param>
        protected override void BuildRowFields(VisualElement container, FactionRowData data, int rowIndex)
        {
            // ID field
            TextField idField = new TextField();
            idField.value = data.ID;
            idField.RegisterValueChangedCallback(evt => data.ID = evt.newValue);
            container.Add(CreateCell(0, idField));

            container.Add(CreateSeparator());

            // Display Name field
            TextField displayNameField = new TextField();
            displayNameField.value = data.DisplayName;
            displayNameField.RegisterValueChangedCallback(evt => data.DisplayName = evt.newValue);
            container.Add(CreateCell(1, displayNameField));

            container.Add(CreateSeparator());

            // Description field
            TextField descriptionField = new TextField();
            descriptionField.value = data.Description;
            descriptionField.RegisterValueChangedCallback(evt => data.Description = evt.newValue);
            container.Add(CreateCell(2, descriptionField));
        }

        /// <summary>
        /// Creates asset files based on the provided row data and saves them to the specified folder path
        /// </summary>
        /// <param name="baseFolderPath">The directory path where the generated assets will be stored</param>
        /// <returns>The total count of assets successfully created</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();

            for (int i = 0; i < Rows.Count; i++)
            {
                FactionRowData data = Rows[i];

                // Skip invalid rows
                if (!data.IsValid) continue;

                // Create the asset
                FactionSO asset = ScriptableObject.CreateInstance<FactionSO>();

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
                    fileBuilder.Append("Faction_");
                    fileBuilder.Append(i);
                }
                else
                {
                    fileBuilder.Append("Faction_");
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
        /// Retrieves the unique identifier (ID) associated with the specified row data
        /// </summary>
        /// <param name="data">The faction row data from which the ID will be extracted</param>
        /// <returns>The unique identifier of the specified faction row data</returns>
        protected override string GetRowID(FactionRowData data) => data.ID;

        /// <summary>
        /// Sets the unique identifier (ID) for a given row of faction data to the specified new ID value
        /// </summary>
        /// <param name="row">The faction data row whose ID needs to be updated</param>
        /// <param name="newID">The new unique identifier to assign to the row</param>
        protected override void SetRowID(FactionRowData row, string newID) => row.ID = newID;
    }
}