using System;
using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Tabs;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    /// <summary>
    /// Represents a builder for creating, configuring, and managing trait-based condition rows
    /// within the batch asset creation editor
    /// </summary>
    public class TraitConditionBuilder : BaseConditionRowBuilder
    {
        public override string DisplayName => "Trait Conditions";
        public override string AssetTypeName => "TraitConditionSO";
        
        public override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("Role ID", flexGrow: 1),
            new ColumnDefinition("Trait ID", width: 300),
            new ColumnDefinition("Must Have", 80, tooltip: "If checked, the role must have the trait; if unchecked, the role must not have the trait")
        };

        /// <summary>
        /// Creates and returns a new instance of a condition row data object associated with a trait requirement
        /// </summary>
        /// <returns>A new instance of <see cref="TraitConditionRowData"/>, representing the trait condition row data</returns>
        public override BaseConditionRowData CreateRowData() => new TraitConditionRowData();

        /// <summary>
        /// Builds and populates the row fields of the user interface for editing trait condition data
        /// </summary>
        /// <param name="container">The UI container to which the row fields will be added</param>
        /// <param name="data">The data object containing the trait condition information to be displayed and edited</param>
        /// <param name="onDataChanged">A callback that is invoked whenever the data values are modified</param>
        public override void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged)
        {
            // Exit case - invalid data type
            if (data is not TraitConditionRowData traitData) return;
            
            // Role ID field
            TextField roleField = new TextField();
            roleField.style.whiteSpace = WhiteSpace.NoWrap;
            roleField.value = traitData.RoleID;
            roleField.RegisterValueChangedCallback(evt =>
            {
                traitData.RoleID = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(0, roleField));
            
            container.Add(CreateSeparator());
            
            // Trait ID field
            TextField traitField = new TextField();
            traitField.style.whiteSpace = WhiteSpace.NoWrap;
            traitField.value = traitData.TraitID;
            traitField.RegisterValueChangedCallback(evt =>
            {
                traitData.TraitID = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(1, traitField));
            
            container.Add(CreateSeparator());
            
            // Must have toggle
            Toggle mustHaveToggle = new Toggle();
            mustHaveToggle.value = traitData.MustHaveTrait;
            mustHaveToggle.RegisterValueChangedCallback(evt =>
            {
                traitData.MustHaveTrait = evt.newValue;
                onDataChanged?.Invoke();
            });
            container.Add(CreateCell(2, mustHaveToggle));
        }

        /// <summary>
        /// Creates a new asset of type <see cref="TraitConditionSO"/> using the provided data and saves it to the specified folder path
        /// </summary>
        /// <param name="data">The condition data used to populate the new asset. Must be of type <see cref="TraitConditionRowData"/> and valid</param>
        /// <param name="folderPath">The folder path where the asset will be created and stored</param>
        /// <returns>Returns <c>true</c> if the asset was successfully created and saved, otherwise <c>false</c></returns>
        public override bool CreateAsset(BaseConditionRowData data, string folderPath)
        {
            // Exit case - invalid data type
            if (data is not TraitConditionRowData { IsValid: true } traitData)
                return false;

            // Create asset
            TraitConditionSO asset = ScriptableObject.CreateInstance<TraitConditionSO>();

            // Set values
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("roleID").stringValue = traitData.RoleID;
            serialized.FindProperty("traitID").stringValue = traitData.TraitID;
            serialized.FindProperty("mustHaveTrait").boolValue = traitData.MustHaveTrait;
            serialized.ApplyModifiedProperties();

            // Build file name
            StringBuilder fileBuilder = new StringBuilder();
            fileBuilder.Append("Condition_");
            fileBuilder.Append(traitData.RoleID);
            fileBuilder.Append("_");
            fileBuilder.Append(traitData.MustHaveTrait ? "Has" : "NotHas");
            fileBuilder.Append("_");
            fileBuilder.Append(traitData.TraitID);
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