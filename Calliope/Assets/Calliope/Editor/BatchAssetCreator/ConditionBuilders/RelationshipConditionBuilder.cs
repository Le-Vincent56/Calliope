using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Tabs;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    public class RelationshipConditionBuilder : BaseConditionRowBuilder
    {
        private static readonly List<string> RelationshipTypeOptions = new List<string>(Enum.GetNames(typeof(RelationshipType)));
        public override string DisplayName => "Relationship Conditions";
        public override string AssetTypeName => "RelationshipConditionSO";

        public override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("From Role ID", flexGrow: 1),
            new ColumnDefinition("To Role ID", width: 300),
            new ColumnDefinition("Type", 120, tooltip: "The relationship type between the two roles"),
            new ColumnDefinition("Threshold", 80, tooltip: "The minimum threshold for the relationship to be considered valid"),
            new ColumnDefinition(">=", 40, tooltip: "If checked: >=; if unchecked: <=")
        };

        /// <summary>
        /// Creates a new instance of <see cref="RelationshipConditionRowData"/> to represent a single row of condition data specific to relationships
        /// </summary>
        /// <returns>An instance of <see cref="RelationshipConditionRowData"/> initialized with default values</returns>
        public override BaseConditionRowData CreateRowData() => new RelationshipConditionRowData();

        /// <summary>
        /// Generates the user interface fields for a relationship condition row and handles field interactions
        /// </summary>
        /// <param name="container">The UI container to which the generated fields will be added</param>
        /// <param name="data">The condition data used to populate the fields. Must be of type <see cref="RelationshipConditionRowData"/></param>
        /// <param name="onDataChanged">A callback action invoked whenever a field value is modified</param>
        public override void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged)
        {
            // Exit case - invalid data type
            if (data is not RelationshipConditionRowData relData) return;

            // From Role ID field
            TextField fromRoleField = new TextField();
            fromRoleField.value = relData.FromRoleID;
            fromRoleField.RegisterValueChangedCallback(evt =>
            {
                relData.FromRoleID = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(0, fromRoleField));
            
            container.Add(CreateSeparator());

            // To Role ID field
            TextField toRoleField = new TextField();
            toRoleField.value = relData.ToRoleID;
            toRoleField.RegisterValueChangedCallback(evt =>
            {
                relData.ToRoleID = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(1, toRoleField));
            
            container.Add(CreateSeparator());
            
            // Relationship Type dropdown
            PopupField<string> relationshipTypeField = new PopupField<string>(
                RelationshipTypeOptions,
                relData.RelationshipTypeIndex
            );
            relationshipTypeField.RegisterValueChangedCallback(evt =>
            {
                relData.RelationshipTypeIndex = relationshipTypeField.index;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(2, relationshipTypeField));
            
            container.Add(CreateSeparator());

            // Threshold field
            FloatField thresholdField = new FloatField();
            thresholdField.value = relData.Threshold;
            thresholdField.RegisterValueChangedCallback(evt =>
            {
                relData.Threshold = Mathf.Clamp(evt.newValue, 0f, 100f);
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(3, thresholdField));
            
            container.Add(CreateSeparator());

            // Greater than or equal toggle
            Toggle gteToggle = new Toggle();
            gteToggle.value = relData.GreaterThanOrEqual;
            gteToggle.RegisterValueChangedCallback(evt =>
            {
                relData.GreaterThanOrEqual = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(4, gteToggle));
        }

        /// <summary>
        /// Creates a new asset based on the provided data and saves it to the specified folder path
        /// </summary>
        /// <param name="data">
        /// The condition data used to initialize the asset; must be of type
        /// <see cref="RelationshipConditionRowData"/> and valid for the operation
        /// </param>
        /// <param name="folderPath">
        /// The file system path where the asset will be saved
        /// </param>
        /// <returns>
        /// Returns true if the asset is created and saved successfully; otherwise, false
        /// </returns>
        public override bool CreateAsset(BaseConditionRowData data, string folderPath)
        {
            // Exit case - invalid data type
            if (data is not RelationshipConditionRowData { IsValid: true } relData) return false;

            // Create asset
            RelationshipConditionSO asset = ScriptableObject.CreateInstance<RelationshipConditionSO>();

            // Set values
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("fromRoleID").stringValue = relData.FromRoleID;
            serialized.FindProperty("toRoleID").stringValue = relData.ToRoleID;
            serialized.FindProperty("relationshipType").enumValueIndex = relData.RelationshipTypeIndex;
            serialized.FindProperty("threshold").floatValue = relData.Threshold;
            serialized.FindProperty("greaterThanOrEqual").boolValue = relData.GreaterThanOrEqual;
            serialized.ApplyModifiedProperties();

            // Build file name
            StringBuilder fileBuilder = new StringBuilder();
            fileBuilder.Append("Condition_");
            fileBuilder.Append(relData.FromRoleID);
            fileBuilder.Append("_");
            fileBuilder.Append(relData.ToRoleID);
            fileBuilder.Append("_");
            fileBuilder.Append(relData.GreaterThanOrEqual ? "GTE" : "LTE");
            fileBuilder.Append("_");
            fileBuilder.Append((int)relData.Threshold);
            fileBuilder.Append(".asset");

            // Build path
            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(folderPath);
            pathBuilder.Append("/");
            pathBuilder.Append(fileBuilder);

            AssetDatabase.CreateAsset(asset, pathBuilder.ToString());
            return true;
        }
    }
}