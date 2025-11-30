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

        /// <summary>
        /// Builds UI fields for a single row of TraitRowData within the specified container
        /// </summary>
        /// <param name="container">The container in which the UI elements for the row will be added</param>
        /// <param name="data">The TraitRowData object containing the information to populate the UI fields</param>
        protected override void BuildRowFields(VisualElement container, TraitRowData data)
        {
            // Add the ID field
            TextField idField = new TextField("ID");
            idField.value = data.ID;
            idField.style.width = 150;
            idField.RegisterValueChangedCallback(evt => data.ID = evt.newValue);
            container.Add(idField);

            // Add the Display Name field
            TextField displayNameField = new TextField("Display Name");
            displayNameField.value = data.DisplayName;
            displayNameField.style.width = 200;
            displayNameField.RegisterValueChangedCallback(evt => data.DisplayName = evt.newValue);
            container.Add(displayNameField);

            // Add the Category field
            PopupField<string> categoryField = new PopupField<string>(
                "Category",
                new List<string>(Enum.GetNames(typeof(TraitCategory))), 
                data.CategoryIndex
            );
            categoryField.RegisterValueChangedCallback(evt => data.CategoryIndex = categoryField.index);
            container.Add(categoryField);
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
    }
}