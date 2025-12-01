using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a specific implementation of the BaseBatchTab for handling "Role" data types;
    /// this class is used to define the behavior and UI for creating batch assets related to roles
    /// within a specified editor framework
    /// </summary>
    public class RoleBatchTab : BaseBatchTab<RoleRowData>
    {
        public override string TabName => "Roles";
        protected override string SubfolderName => "Roles";

        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("ID", 100),
            new ColumnDefinition("Display Name", 140),
            new ColumnDefinition("Required Traits", flexGrow: 1, tooltip: "Comma-separated trait IDs (e.g., brave, strong)"),
            new ColumnDefinition("Forbidden Traits", flexGrow: 1, tooltip: "Comma-separated trait IDs (e.g., brave, strong)")
        };

        /// <summary>
        /// Builds and populates the row fields in the given container using the specified data
        /// </summary>
        /// <param name="container">The visual element container where the row fields will be added</param>
        /// <param name="data">The data object that provides the values for the row fields</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected override void BuildRowFields(VisualElement container, RoleRowData data, int rowIndex)
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
            
            // Required Traits field
            TextField requiredField = new TextField();
            requiredField.value = data.RequiredTraits;
            requiredField.RegisterValueChangedCallback(evt => data.RequiredTraits = evt.newValue);
            container.Add(CreateCell(2, requiredField));
            
            container.Add(CreateSeparator());
            
            // Forbidden trait fields
            TextField forbiddenField = new TextField();
            forbiddenField.value = data.ForbiddenTraits;
            forbiddenField.RegisterValueChangedCallback(evt => data.ForbiddenTraits = evt.newValue);
            container.Add(CreateCell(3, forbiddenField));
        }

        /// <summary>
        /// Creates assets in the specified base folder and returns the count of created assets
        /// </summary>
        /// <param name="baseFolderPath">The base folder path where the assets will be created</param>
        /// <returns>The number of assets successfully created</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();

            for (int i = 0; i < Rows.Count; i++)
            {
                RoleRowData data = Rows[i];

                // Skip over invalid rows
                if (!data.IsValid) continue;

                SceneRoleSO asset = ScriptableObject.CreateInstance<SceneRoleSO>();

                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("roleID").stringValue = data.ID;
                serialized.FindProperty("displayName").stringValue = data.DisplayName;
                
                // Parse required traits
                if (!string.IsNullOrEmpty(data.RequiredTraits))
                {
                    string[] traits = ParseCommaSeparated(data.RequiredTraits);
                    SerializedProperty property = serialized.FindProperty("requiredTraits");
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
                    fileBuilder.Append("Role_");
                    fileBuilder.Append(i);
                }
                else
                {
                    fileBuilder.Append("Role_");
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