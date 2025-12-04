using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Editor.BatchAssetCreator.Attributes;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Tabs;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    /// <summary>
    /// Represents a condition builder designed to handle "Scene Context" conditions, allowing the configuration of logic
    /// based on specific scene-related keys, comparison operators, and target values; this class defines how
    /// these conditions are structured and provides tools for managing associated row data, UI elements, and asset creation
    /// </summary>
    [ConditionBuilder(order: 40)]
    public class SceneContextConditionBuilder : BaseConditionRowBuilder
    {
        private static readonly string[] ComparisonNames = Enum.GetNames(typeof(ContextValueComparison));

        public override string DisplayName => "Scene Context";
        public override string AssetTypeName => "SceneContextConditionSO";

        public override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("Context Key", flexGrow: 1, tooltip: "The key to look up (e.g., beat.opening.visited, tension_level)"),
            new ColumnDefinition("Comparison Type", 120),
            new ColumnDefinition("Target Value", 150, tooltip: "Value to compare against (for Equals, GreaterThan, etc.")
        };

        /// <summary>
        /// Creates a new instance of row data specific to the condition builder implementation
        /// </summary>
        /// <returns>A new instance of a class derived from BaseConditionRowData appropriate for this builder</returns>
        public override BaseConditionRowData CreateRowData() => new SceneContextConditionRowData();

        /// <summary>
        /// Constructs and populates UI row fields in the container based on the provided condition row data
        /// </summary>
        /// <param name="container">The UI container where the row fields will be added</param>
        /// <param name="data">The condition row data used to initialize field values</param>
        /// <param name="onDataChanged">The callback invoked when any field value changes</param>
        public override void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged)
        {
            // Exit case - invalid data type
            if (data is not SceneContextConditionRowData contextData) return;

            // Context Key field
            TextField keyField = new TextField();
            keyField.value = contextData.Key;
            keyField.RegisterValueChangedCallback(evt =>
            {
                contextData.Key = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(0, keyField));

            container.Add(CreateSeparator());

            // Comparison dropdown
            PopupField<string> comparisonField = new PopupField<string>(
                new List<string>(ComparisonNames),
                contextData.ComparisonIndex
            );
            comparisonField.RegisterValueChangedCallback(evt =>
            {
                contextData.ComparisonIndex = comparisonField.index;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(1, comparisonField));

            container.Add(CreateSeparator());

            // Target Value field
            TextField targetField = new TextField();
            targetField.value = contextData.TargetValue;
            targetField.RegisterValueChangedCallback(evt =>
            {
                contextData.TargetValue = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(2, targetField));
        }

        /// <summary>
        /// Creates an asset based on the provided condition row data and saves it to the specified folder path
        /// </summary>
        /// <param name="data">The condition row data used to populate the asset's properties</param>
        /// <param name="folderPath">The file system path where the created asset will be saved</param>
        /// <returns>A boolean indicating whether the asset creation was successful</returns>
        public override bool CreateAsset(BaseConditionRowData data, string folderPath)
        {
            // Exit case - invalid data type
            if (data is not SceneContextConditionRowData { IsValid: true } contextData)
                return false;

            // Create the asset
            SceneContextConditionSO asset = ScriptableObject.CreateInstance<SceneContextConditionSO>();

            // Set values
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("key").stringValue = contextData.Key;
            serialized.FindProperty("comparison").enumValueIndex = contextData.ComparisonIndex;
            serialized.FindProperty("targetValue").stringValue = contextData.TargetValue;
            serialized.ApplyModifiedProperties();

            // Build the file name
            StringBuilder fileName = new StringBuilder();
            fileName.Append("Condition_Context_");
            fileName.Append(SanitizeForFileName(contextData.Key));
            fileName.Append("_");
            fileName.Append(ComparisonNames[contextData.ComparisonIndex]);
            fileName.Append(".asset");
            
            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(folderPath);
            pathBuilder.Append("/");
            pathBuilder.Append(fileName);
            AssetDatabase.CreateAsset(asset, pathBuilder.ToString());
            return true;
        }

        /// <summary>
        /// Sanitizes the input string to make it safe for use as a file name by replacing invalid or undesirable characters
        /// </summary>
        /// <param name="input">The original string that needs sanitization</param>
        /// <returns>A sanitized string safe for use as a file name, with invalid or undesirable characters replaced</returns>
        private string SanitizeForFileName(string input)
        {
            return string.IsNullOrEmpty(input) ? "unknown" : input.Replace(".", "_").Replace(" ", "_");
        }
    }
}