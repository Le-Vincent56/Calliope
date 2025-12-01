using System.Collections.Generic;
using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a batch tab specifically used for managing and creating
    /// condition-based assets within the batch asset creation process of the editor
    /// </summary>
    public class ConditionBatchTab : BaseBatchTab<ConditionRowData>
    {
        public override string TabName => "Conditions";
        protected override string SubfolderName => "Conditions";

        private static readonly List<string> ConditionTypeOptions = new List<string>
        {
            "Trait Condition",
            "Relationship Condition"
        };

        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("Type", 100),
            new ColumnDefinition("Role ID", 120, tooltip: "The role ID to check (e.g., speaker, responder)"),
            new ColumnDefinition("Trait ID", flexGrow: 1, tooltip: "The trait ID to check (e.g., brave, strong)"),
            new ColumnDefinition("Must Have", 80)
        };

        /// <summary>
        /// Builds the row fields for the UI container using the provided data
        /// by configuring input elements such as dropdowns, text fields, and toggles,
        /// and binds their values to the properties of the data instance
        /// </summary>
        /// <param name="container">The UI container where the row fields will be added</param>
        /// <param name="data">The data object containing values to populate the fields and store updated values</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected override void BuildRowFields(VisualElement container, ConditionRowData data, int rowIndex)
        {
            // Condition Type field
            PopupField<string> typeField = new PopupField<string>(
                ConditionTypeOptions,
                data.ConditionType
            );
            typeField.RegisterValueChangedCallback(evt => data.ConditionType = typeField.index);
            container.Add(CreateCell(0, typeField));
            
            container.Add(CreateSeparator());
            
            // Role ID field
            TextField roleField = new TextField();
            roleField.value = data.RoleID;
            roleField.RegisterValueChangedCallback(evt => data.RoleID = evt.newValue);
            container.Add(CreateCell(1, roleField));
            
            container.Add(CreateSeparator());
            
            // Trait ID field
            TextField traitField = new TextField();
            traitField.value = data.TraitID;
            traitField.RegisterValueChangedCallback(evt => data.TraitID = evt.newValue);
            container.Add(CreateCell(2, traitField));
            
            container.Add(CreateSeparator());
            
            // Must have toggle
            Toggle mustHaveToggle = new Toggle();
            mustHaveToggle.value = data.MustHaveTrait;
            mustHaveToggle.RegisterValueChangedCallback(evt => data.MustHaveTrait = evt.newValue);
            container.Add(CreateCell(3, mustHaveToggle));
        }

        /// <summary>
        /// Creates assets based on the provided rows and saves them in the specified folder path;
        /// skips invalid rows during the process and returns the count of successfully created assets
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder where the assets should be created</param>
        /// <returns>The number of assets successfully created</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();
            
            for (int i = 0; i < Rows.Count; i++)
            {
                ConditionRowData data = Rows[i];

                // Skip over invalid rows
                if (!data.IsValid) continue;

                if (data.ConditionType == 0)
                {
                    TraitConditionSO asset = ScriptableObject.CreateInstance<TraitConditionSO>();
                    
                    // Set values
                    SerializedObject serialized = new SerializedObject(asset);
                    serialized.FindProperty("roleID").stringValue = data.RoleID;
                    serialized.FindProperty("traitID").stringValue = data.TraitID;
                    serialized.FindProperty("mustHaveTrait").boolValue = data.MustHaveTrait;
                    serialized.ApplyModifiedProperties();
                    
                    // Create the file name
                    string mustHaveStr = data.MustHaveTrait ? "Has" : "NotHas";
                    fileBuilder.Clear();
                    fileBuilder.Append("Condition_");
                    fileBuilder.Append(data.RoleID);
                    fileBuilder.Append("_");
                    fileBuilder.Append(mustHaveStr);
                    fileBuilder.Append("_");
                    fileBuilder.Append(data.TraitID);
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
                else
                {
                    // TODO: Implement RelationshipConditionSO creation if needed
                }
            }

            return count;
        }
    }
}