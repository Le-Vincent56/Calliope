using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.ValueObjects;
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
    /// Represents a condition builder for creating and managing "Accumulated Relationship" conditions;
    /// this class provides configuration for how these conditions are defined, using column definitions,
    /// associated data structures, and asset creation capabilities
    /// </summary>
    [ConditionBuilder(order: 30)]
    public class AccumulatedRelationshipConditionBuilder : BaseConditionRowBuilder
    {
        private static readonly string[] AggregationNames = Enum.GetNames(typeof(AggregationType));
        private static readonly string[] RelationshipNames = Enum.GetNames(typeof(RelationshipType));

        public override string DisplayName => "Accumulated Relationship";
        public override string AssetTypeName => "AccumulatedRelationshipConditionSO";

        public override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("Role Pairs", flexGrow: 1, tooltip: "Format: from>to, from>to (e.g., sovereign_role>voice_of_war, voice_of_peace>voice_of_war"),
            new ColumnDefinition("Aggregation Type", 100),
            new ColumnDefinition("Relationship Type", 100),
            new ColumnDefinition("Threshold", 80),
            new ColumnDefinition(">=", 40, tooltip: "If checked: >=; if unchecked: <=")
        };

        /// <summary>
        /// Creates and returns a new instance of AccumulatedRelationshipConditionRowData
        /// </summary>
        /// <returns>An instance of AccumulatedRelationshipConditionRowData representing the condition row data</returns>
        public override BaseConditionRowData CreateRowData() => new AccumulatedRelationshipConditionRowData();

        /// <summary>
        /// Builds and populates UI row fields in the given container based on the provided condition row data
        /// </summary>
        /// <param name="container">The UI container where the row fields will be added</param>
        /// <param name="data">The base condition row data used to configure the fields</param>
        /// <param name="onDataChanged">The callback invoked when the condition row data changes</param>
        public override void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged)
        {
            // Exit case - invalid data type
            if (data is not AccumulatedRelationshipConditionRowData accData) return;

            // Role Pairs field (text format: "from>to, from>to")
            TextField pairsField = new TextField();
            pairsField.value = FormatRolePairs(accData.RolePairs);
            pairsField.RegisterValueChangedCallback(evt =>
            {
                accData.RolePairs = ParseRolePairs(evt.newValue);
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(0, pairsField));

            container.Add(CreateSeparator());

            // Aggregation dropdown
            PopupField<string> aggField = new PopupField<string>(
                new List<string>(AggregationNames), 
                accData.AggregationTypeIndex
            );
            aggField.RegisterValueChangedCallback(evt =>
            {
                accData.AggregationTypeIndex = aggField.index;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(1, aggField));

            container.Add(CreateSeparator());

            // Relationship type dropdown
            PopupField<string> relField = new PopupField<string>(
                new List<string>(RelationshipNames), 
                accData.RelationshipTypeIndex
            );
            relField.RegisterValueChangedCallback(evt =>
            {
                accData.RelationshipTypeIndex = relField.index;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(2, relField));

            container.Add(CreateSeparator());

            // Threshold field
            FloatField thresholdField = new FloatField();
            thresholdField.value = accData.Threshold;
            thresholdField.RegisterValueChangedCallback(evt =>
            {
                accData.Threshold = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(3, thresholdField));

            container.Add(CreateSeparator());

            // Greater than or equal toggle
            Toggle gteToggle = new Toggle();
            gteToggle.value = accData.GreaterThanOrEqual;
            gteToggle.RegisterValueChangedCallback(evt =>
            {
                accData.GreaterThanOrEqual = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(4, gteToggle));
        }

        /// <summary>
        /// Creates an asset based on the provided condition row data and saves it to the specified folder path
        /// </summary>
        /// <param name="data">The condition row data containing information needed to create the asset</param>
        /// <param name="folderPath">The folder path where the asset will be saved</param>
        /// <returns>Returns true if the asset was successfully created; otherwise, false</returns>
        public override bool CreateAsset(BaseConditionRowData data, string folderPath)
        {
            // Exit case - invalid data type
            if (data is not AccumulatedRelationshipConditionRowData { IsValid: true } accData)
                return false;

            // Create the asset
            AccumulatedRelationshipConditionSO asset = ScriptableObject.CreateInstance<AccumulatedRelationshipConditionSO>();

            SerializedObject serialized = new SerializedObject(asset);

            // Set role pairs
            SerializedProperty pairsProperty = serialized.FindProperty("rolePairs");
            pairsProperty.arraySize = accData.RolePairs.Count;
            for (int i = 0; i < accData.RolePairs.Count; i++)
            {
                SerializedProperty element = pairsProperty.GetArrayElementAtIndex(i);

                // Set RolePair values
                element.FindPropertyRelative("FromRoleID").stringValue = accData.RolePairs[i].FromRoleID;
                element.FindPropertyRelative("ToRoleID").stringValue = accData.RolePairs[i].ToRoleID;
            }

            // Set other values
            serialized.FindProperty("aggregationType").enumValueIndex = accData.AggregationTypeIndex;
            serialized.FindProperty("relationshipType").enumValueIndex = accData.RelationshipTypeIndex;
            serialized.FindProperty("threshold").floatValue = accData.Threshold;
            serialized.FindProperty("greaterThanOrEqual").boolValue = accData.GreaterThanOrEqual;
            serialized.ApplyModifiedProperties();

            // Build file name
            StringBuilder fileName = new StringBuilder();
            fileName.Append("Condition_AccRel_");
            fileName.Append(AggregationNames[accData.AggregationTypeIndex]);
            fileName.Append("aggr_");
            fileName.Append(RelationshipNames[accData.RelationshipTypeIndex]);
            fileName.Append("rel_");
            fileName.Append(accData.RolePairs.Count);
            fileName.Append("pairs.asset");

            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(folderPath);
            pathBuilder.Append("/");
            pathBuilder.Append(fileName);
            AssetDatabase.CreateAsset(asset, pathBuilder.ToString());
            return true;
        }

        /// <summary>
        /// Converts a list of RolePair objects into a formatted string where each pair is represented as "from>to"
        /// and pairs are separated by commas
        /// </summary>
        /// <param name="pairs">The list of RolePair objects to be formatted; each RolePair contains the from and to role identifiers</param>
        /// <returns>Returns a formatted string containing all role pairs, or an empty string if the list is null or empty</returns>
        private string FormatRolePairs(List<RolePair> pairs)
        {
            // Exit case - no pairs to format
            if (pairs == null || pairs.Count == 0) 
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pairs.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(pairs[i].FromRoleID);
                sb.Append(">");
                sb.Append(pairs[i].ToRoleID);
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Parses a text input representing role pairs and converts it into a list of RolePair objects
        /// </summary>
        /// <param name="text">A string containing role pairs formatted as "from>to, from>to"; for example: "sovereign_role>voice_of_war, voice_of_peace>voice_of_war"</param>
        /// <returns>Returns a list of RolePair objects extracted from the input string; if the input string is empty or invalid, returns an empty list</returns>
        private List<RolePair> ParseRolePairs(string text)
        {
            List<RolePair> result = new List<RolePair>();
            
            // Exit case - no text to parse
            if (string.IsNullOrEmpty(text)) return result;

            string[] pairs = text.Split(',');
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] parts = pairs[i].Trim().Split('>');
                if (parts.Length == 2)
                {
                    result.Add(new RolePair(parts[0].Trim(), parts[1].Trim()));
                }
            }
            return result;
        }
    }
}