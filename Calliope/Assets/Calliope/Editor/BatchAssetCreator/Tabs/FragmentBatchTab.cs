using System.Text;
using Calliope.Editor.BatchAssetCreator.RowData;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents a batch tab specifically designed for creating and managing rows of FragmentRowData;
    /// inherits functionality from BaseBatchTab and provides implementation for fragment-specific asset creation
    /// and UI row construction
    /// </summary>
    public class FragmentBatchTab : BaseBatchTab<FragmentRowData>
    {
        public override string TabName => "Fragments";
        protected override string SubfolderName => "Fragments";

        protected override ColumnDefinition[] Columns => new[]
        {
            new ColumnDefinition("ID", 120),
            new ColumnDefinition("Text", flexGrow: 1),
            new ColumnDefinition("Trait Affinities", 180, tooltip: "Format: trait:weight, trait:weight (e.g., brave:1.5, kind:1.0)")
        };
        
        protected override string AssetTypeName => "DialogueFragmentSO";

        /// <summary>
        /// Constructs and populates fields for a row of FragmentRowData, adding them to the specified container
        /// </summary>
        /// <param name="container">The UI container to which the fields will be added</param>
        /// <param name="data">The FragmentRowData instance containing data for populating the row fields</param>
        /// <param name="rowIndex">The index of the row being built, used for contextual identification</param>
        protected override void BuildRowFields(VisualElement container, FragmentRowData data, int rowIndex)
        {
            // Add the ID field
            TextField idField = new TextField();
            idField.value = data.ID;
            idField.RegisterValueChangedCallback(evt => data.ID = evt.newValue);
            container.Add(CreateCell(0, idField));
            
            container.Add(CreateSeparator());
            
            // Add the Text field
            TextField textField = new TextField();
            textField.value = data.Text;
            textField.RegisterValueChangedCallback(evt => data.Text = evt.newValue);
            container.Add(CreateCell(1, textField));
            
            container.Add(CreateSeparator());
            
            // Add the Trait Affinities field
            TextField affinitiesField = new TextField("Trait Affinities");
            affinitiesField.value = data.TraitAffinities;
            affinitiesField.RegisterValueChangedCallback(evt => data.TraitAffinities = evt.newValue);
            container.Add(CreateCell(2, affinitiesField));
        }

        /// <summary>
        /// Creates asset files for the current set of rows and saves them to the specified folder path
        /// </summary>
        /// <param name="baseFolderPath">The base folder path where the assets will be created; a subfolder may also be created based on the implementation</param>
        /// <returns>The number of assets successfully created and saved.</returns>
        public override int CreateAssets(string baseFolderPath)
        {
            int count = 0;
            string subfolder = EnsureSubfolder(baseFolderPath);
            StringBuilder fileBuilder = new StringBuilder();
            
            for (int i = 0; i < Rows.Count; i++)
            {
                FragmentRowData data = Rows[i];
                
                // Skip over invalid rows
                if (!data.IsValid) continue;

                // Create a new asset
                DialogueFragmentSO asset = ScriptableObject.CreateInstance<DialogueFragmentSO>();
                
                // Set the ID and text
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("id").stringValue = data.ID;
                serialized.FindProperty("text").stringValue = data.Text;
                
                // Parse trait affinities
                if (!string.IsNullOrEmpty(data.TraitAffinities))
                {
                    SerializedProperty affinitiesProperty = serialized.FindProperty("traitAffinities");
                    string[] pairs = data.TraitAffinities.Split(',');
                    affinitiesProperty.arraySize = pairs.Length;

                    for (int j = 0; j < pairs.Length; j++)
                    {
                        string[] parts = pairs[j].Trim().Split(':');
                        
                        // Skip if the pair is invalid
                        if (parts.Length != 2) continue;
                        
                        // Set the trait ID
                        SerializedProperty element = affinitiesProperty.GetArrayElementAtIndex(j);
                        element.FindPropertyRelative("traitID").stringValue = parts[0].Trim();

                        // Skip if the weight is invalid
                        if (!float.TryParse(parts[1].Trim(), out float weight)) continue;
                        
                        // Set the weight
                        element.FindPropertyRelative("weight").floatValue = weight;
                    }
                }
                
                // Save the asset
                serialized.ApplyModifiedProperties();
                
                // Create the file name
                fileBuilder.Clear();
                if (string.IsNullOrEmpty(data.ID))
                {
                    fileBuilder.Append("Fragment_");
                    fileBuilder.Append(i);
                }
                else
                {
                    fileBuilder.Append("Fragment_");
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
                
                // Create the asset
                AssetDatabase.CreateAsset(asset, assetPath);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Retrieves the unique identifier for a specified FragmentRowData instance
        /// </summary>
        /// <param name="data">The FragmentRowData instance for which the unique identifier is being retrieved</param>
        /// <returns>The unique identifier string associated with the provided FragmentRowData instance</returns>
        protected override string GetRowID(FragmentRowData data) => data.ID;
    }
}