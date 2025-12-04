using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Editor.BatchAssetCreator.Attributes;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Tabs;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    /// <summary>
    /// Responsible for building and managing condition rows specific to faction majority evaluations
    /// within the Batch Asset Creator tool; this builder handles the creation and configuration
    /// of assets and UI fields for defining conditions involving faction majority logic
    /// </summary>
    [ConditionBuilder(order: 50)]
    public class FactionMajorityConditionBuilder : BaseConditionRowBuilder
    {
        private static readonly string[] ComparisonNames = Enum.GetNames(typeof(FactionComparisonType));

        public override string DisplayName => "Faction Majority";
        public override string AssetTypeName => "FactionMajorityConditionSO";

        public override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("Faction ID", flexGrow: 1, tooltip: "The faction ID to evaluate (e.g., sealed_circle, unbound)"),
            new ColumnDefinition("Comparison Type", 120),
            new ColumnDefinition("Count", 80, tooltip: "Count for AtLeast, Exactly, MoreThan, LessThan comparisons")
        };

        /// <summary>
        /// Creates and returns a new instance of FactionMajorityConditionRowData, representing
        /// the specific condition data for faction majority cases
        /// </summary>
        /// <returns>A new instance of FactionMajorityConditionRowData</returns>
        public override BaseConditionRowData CreateRowData() => new FactionMajorityConditionRowData();

        /// <summary>
        /// Constructs and populates the user interface elements for a row based on the specified condition data, enabling dynamic user input and updates
        /// </summary>
        /// <param name="container">The container to which the generated UI elements will be added</param>
        /// <param name="data">The condition row data used to determine how the row fields are constructed</param>
        /// <param name="onDataChanged">The callback invoked when the row data is modified</param>
        public override void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged)
        {
            // Exit case - invalid data type
            if (data is not FactionMajorityConditionRowData factionData) return;

            // Faction field
            ObjectField factionField = new ObjectField();
            factionField.objectType = typeof(FactionSO);
            factionField.value = factionData.Faction;
            factionField.RegisterValueChangedCallback(evt =>
            {
                factionData.Faction = evt.newValue as FactionSO;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(0, factionField));

            container.Add(CreateSeparator());

            // Comparison dropdown
            PopupField<string> compField = new PopupField<string>(
                new List<string>(ComparisonNames),
                factionData.ComparisonTypeIndex
            );
            compField.RegisterValueChangedCallback(evt =>
            {
                factionData.ComparisonTypeIndex = compField.index;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(1, compField));

            container.Add(CreateSeparator());

            // Count field
            IntegerField countField = new IntegerField();
            countField.value = factionData.Count;
            countField.RegisterValueChangedCallback(evt =>
            {
                factionData.Count = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(2, countField));
        }

        /// <summary>
        /// Creates an asset based on the given row data and saves it in the specified folder path
        /// </summary>
        /// <param name="data">The row data used to configure the asset; must be of type FactionMajorityConditionRowData and valid</param>
        /// <param name="folderPath">The file system path to the folder where the asset will be created and saved</param>
        /// <returns>True if the asset was successfully created and saved; otherwise, false</returns>
        public override bool CreateAsset(BaseConditionRowData data, string folderPath)
        {
            if (data is not FactionMajorityConditionRowData { IsValid: true } factionData)
                return false;

            FactionMajorityConditionSO asset = ScriptableObject.CreateInstance<FactionMajorityConditionSO>();

            // Set values
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("targetFaction").objectReferenceValue = factionData.Faction;
            serialized.FindProperty("comparisonType").enumValueIndex = factionData.ComparisonTypeIndex;
            serialized.FindProperty("count").intValue = factionData.Count;
            serialized.ApplyModifiedProperties();

            // Build file name
            StringBuilder fileName = new StringBuilder();
            fileName.Append("Condition_Faction_");
            fileName.Append(factionData.Faction.ID);
            fileName.Append("_");
            fileName.Append(ComparisonNames[factionData.ComparisonTypeIndex]);
            fileName.Append(".asset");

            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(folderPath);
            pathBuilder.Append("/");
            pathBuilder.Append(fileName);
            AssetDatabase.CreateAsset(asset, pathBuilder.ToString());
            return true;
        }
    }
}