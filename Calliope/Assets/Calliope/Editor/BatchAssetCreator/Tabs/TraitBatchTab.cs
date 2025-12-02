using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a batch asset creation tab specifically for managing traits;
    /// it provides functionality for defining trait-related data, managing rows of
    /// trait information, and creating asset files for those traits
    /// </summary>
    public class TraitBatchTab : BaseBatchTab<TraitRowData>
    {
        public override string TabName => "Traits";
        protected override string SubfolderName => "Traits";

        private static readonly List<string> CategoryOptions = new List<string>(Enum.GetNames(typeof(TraitCategory)));
        
        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("ID", 120),
            new ColumnDefinition("Display Name", flexGrow: 1),
            new ColumnDefinition("Category", 140)
        };
        
        protected override string AssetTypeName => "TraitSO";

        /// <summary>
        /// Builds UI fields for a single row of TraitRowData within the specified container
        /// </summary>
        /// <param name="container">The container in which the UI elements for the row will be added</param>
        /// <param name="data">The TraitRowData object containing the information to populate the UI fields</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected override void BuildRowFields(VisualElement container, TraitRowData data, int rowIndex)
        {
            // Add the ID field
            TextField idField = new TextField();
            idField.value = data.ID;
            idField.RegisterValueChangedCallback(evt => data.ID = evt.newValue);
            container.Add(CreateCell(0, idField));
            
            container.Add(CreateSeparator());

            // Add the Display Name field
            TextField displayNameField = new TextField();
            displayNameField.value = data.DisplayName;
            displayNameField.RegisterValueChangedCallback(evt => data.DisplayName = evt.newValue);
            container.Add(CreateCell(1, displayNameField));

            container.Add(CreateSeparator());
            
            // Add the Category field
            PopupField<string> categoryField = new PopupField<string>(
                CategoryOptions,
                data.CategoryIndex
            );
            categoryField.RegisterValueChangedCallback(evt => data.CategoryIndex = categoryField.index);
            container.Add(CreateCell(2, categoryField));
        }

        /// <summary>
        /// Creates asset files for valid rows in the current tab and saves them to the specified folder path
        /// </summary>
        /// <param name="baseFolderPath">The base folder path where the assets will be created; a subfolder specific to this tab will be used</param>
        /// <returns>The number of assets successfully created and saved</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();
            
            for (int i = 0; i < Rows.Count; i++)
            {
                TraitRowData data = Rows[i];
                
                // Skip over invalid rows
                if (!data.IsValid) continue;

                TraitSO asset = ScriptableObject.CreateInstance<TraitSO>();

                // Set values
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("id").stringValue = data.ID;
                serialized.FindProperty("displayName").stringValue = data.DisplayName;
                serialized.FindProperty("category").enumValueIndex = data.CategoryIndex;
                
                // Save the asset
                serialized.ApplyModifiedProperties();

                // Create the file name
                fileBuilder.Clear();
                if (string.IsNullOrEmpty(data.ID))
                {
                    fileBuilder.Append("Trait_");
                    fileBuilder.Append(i);
                }
                else
                {
                    fileBuilder.Append("Trait_");
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
        /// Retrieves the unique identifier for the specified row data
        /// </summary>
        /// <param name="data">The TraitRowData object containing the information of the row</param>
        /// <returns>A string representing the unique ID of the row</returns>
        protected override string GetRowID(TraitRowData data) => data.ID;

        /// <summary>
        /// Sets a new ID for the specified row of TraitRowData
        /// </summary>
        /// <param name="row">The TraitRowData object whose ID is to be updated</param>
        /// <param name="newID">The new ID to assign to the row</param>
        protected override void SetRowID(TraitRowData row, string newID) => row.ID = newID;
    }
}