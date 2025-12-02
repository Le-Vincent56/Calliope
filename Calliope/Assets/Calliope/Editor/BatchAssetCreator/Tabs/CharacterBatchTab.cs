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
    /// Represents a UI tab for batch editing and creating character assets within the editor;
    /// inherits from <see cref="BaseBatchTab{TRowData}"/> and provides functionality specific
    /// to handling character row data
    /// </summary>
    public class CharacterBatchTab : BaseBatchTab<CharacterRowData>
    {
        public override string TabName => "Characters";
        protected override string SubfolderName => "Characters";

        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("ID", 100),
            new ColumnDefinition("Display Name", 140),
            new ColumnDefinition("Pronouns", 100),
            new ColumnDefinition("Traits", flexGrow: 1, tooltip: "Comma-separated trait IDs (e.g., brave, kind, wise)")
        };

        protected override string AssetTypeName => "CharacterSO";

        private static readonly List<string> PronounOptions = new List<string>
        {
            "They/Them",
            "He/Him",
            "She/Her"
        };

        /// <summary>
        /// Builds and adds UI fields for editing the properties of a character row within the specified container
        /// </summary>
        /// <param name="container">The container element to which the generated fields will be added</param>
        /// <param name="data">The character row data used to initialize and update the field values</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected override void BuildRowFields(VisualElement container, CharacterRowData data, int rowIndex)
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
            
            // Pronoun fields
            PopupField<string> pronounField = new PopupField<string>(
                "Pronouns", 
                PronounOptions, 
                data.PronounIndex
            );
            pronounField.RegisterValueChangedCallback(evt => data.PronounIndex = pronounField.index);
            container.Add(CreateCell(2, pronounField));
            
            container.Add(CreateSeparator());
            
            // Traits field
            TextField traitsField = new TextField();
            traitsField.value = data.Traits;
            traitsField.RegisterValueChangedCallback(evt => data.Traits = evt.newValue);
            container.Add(CreateCell(3, traitsField));
        }

        /// <summary>
        /// Creates assets in the specified base folder path and returns the count of assets created
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder where the assets will be created</param>
        /// <returns>The number of assets successfully created</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();

            for (int i = 0; i < Rows.Count; i++)
            {
                CharacterRowData data = Rows[i];

                // Skip over invalid rows
                if (!data.IsValid) continue;

                // Create the asset
                CharacterSO asset = ScriptableObject.CreateInstance<CharacterSO>();

                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("id").stringValue = data.ID;
                serialized.FindProperty("displayName").stringValue = data.DisplayName;
                serialized.FindProperty("pronouns").enumValueIndex = data.PronounIndex;
                
                // Parse traits
                if (!string.IsNullOrEmpty(data.Traits))
                {
                    string[] traits = ParseCommaSeparated(data.Traits);
                    SerializedProperty property = serialized.FindProperty("traitIDs");
                    property.arraySize = traits.Length;
                    for (int j = 0; j < traits.Length; j++)
                    {
                        property.GetArrayElementAtIndex(j).stringValue = traits[j];
                    }
                }
                
                serialized.ApplyModifiedProperties();
                
                // Create the file name
                fileBuilder.Clear();
                if (string.IsNullOrEmpty(data.ID))
                {
                    fileBuilder.Append("Character_");
                    fileBuilder.Append(i);
                }
                else
                {
                    fileBuilder.Append("Character_");
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
        /// Retrieves the unique identifier of a character row from the provided data
        /// </summary>
        /// <param name="data">The character row data containing the identifier to retrieve</param>
        /// <returns>The unique identifier string of the character row</returns>
        protected override string GetRowID(CharacterRowData data) => data.ID;

        /// <summary>
        /// Sets the unique identifier for the specified character row data
        /// </summary>
        /// <param name="row">The character row data to which the ID will be assigned</param>
        /// <param name="id">The new unique identifier to assign to the row</param>
        protected override void SetRowID(CharacterRowData row, string id) => row.ID = id;
    }
}