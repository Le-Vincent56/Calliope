using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Handles creation of Calliope assets with proper configuration per asset type
    /// </summary>
    public static class AssetCreator
    {
        /// <summary>
        /// Configuration for an asset type
        /// </summary>
        private class AssetTypeConfig
        {
            public string FolderName { get; }
            public bool MakeAddressable { get; }
            public string AddressableLabel { get; }

            public AssetTypeConfig(string folderName, bool makeAddressable, string addressableLabel)
            {
                FolderName = folderName;
                MakeAddressable = makeAddressable;
                AddressableLabel = addressableLabel;
            }
        }
        
        private const string ContentRoot = "Content/";

        private static readonly Dictionary<Type, AssetTypeConfig> _assetConfigs = new Dictionary<Type, AssetTypeConfig>
        {
            { typeof(SceneRoleSO), new AssetTypeConfig("Roles", false, null) },
            { typeof(VariationSetSO), new AssetTypeConfig("Variation Sets", true, "Variation Set") },
            { typeof(SceneTemplateSO), new AssetTypeConfig("Scene Templates", true, "Scene") },
            { typeof(TraitSO), new AssetTypeConfig("Traits", true, "Trait") },
            { typeof(CharacterSO), new AssetTypeConfig("Characters", true, "Character") },
            { typeof(DialogueFragmentSO), new AssetTypeConfig("Dialogue Fragments", true, "Dialogue Fragment") }
        };

        /// <summary>
        /// Retrieves the default folder path for the specified type of asset
        /// </summary>
        /// <typeparam name="T">The type of the asset, which must inherit from ScriptableObject</typeparam>
        /// <returns>The folder path as a string, or the content root path if no specific configuration exists for the asset type</returns>
        public static string GetDefaultFolder<T>() where T : ScriptableObject
        {
            // Exit case - no configuration exists for the asset type
            if (!_assetConfigs.TryGetValue(typeof(T), out AssetTypeConfig config)) 
                return ContentRoot;
            
            // Build the path
            StringBuilder pathBuilder = new StringBuilder(ContentRoot);
            pathBuilder.Append(ContentRoot);
            pathBuilder.Append("/");
            pathBuilder.Append(config.FolderName);
            
            return pathBuilder.ToString();
        }

        /// <summary>
        /// Creates a new asset of the specified type, optionally initializing it and saving it at a user-selected location
        /// </summary>
        /// <typeparam name="T">The type of the asset to create, which must inherit from ScriptableObject</typeparam>
        /// <param name="suggestedName">The default suggested name of the new asset file, excluding the file extension</param>
        /// <param name="setupAction">An optional action to configure the newly created asset; it receives the asset instance and its serialized representation</param>
        /// <returns>The newly created asset of type T, or null if the user cancels the save operation</returns>
        public static T CreateAsset<T>(string suggestedName, Action<T, SerializedObject> setupAction = null)
            where T : ScriptableObject
        {
            // Get the default folder
            string defaultFolder = GetDefaultFolder<T>();
            
            // Ensure the folder exists
            EnsureFolderExists(defaultFolder);
            
            // Prompt for save location
            StringBuilder pathBuilder = new StringBuilder();
            pathBuilder.Append(defaultFolder);
            pathBuilder.Append("/");
            pathBuilder.Append(suggestedName ?? "New");
            pathBuilder.Append(typeof(T).Name);
            pathBuilder.Append(".asset");
            string defaultPath = pathBuilder.ToString();
            
            StringBuilder titleBuilder = new StringBuilder();
            titleBuilder.Append("Create new ");
            titleBuilder.Append(typeof(T).Name);
            
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append("Choose where to save the new ");
            messageBuilder.Append(typeof(T).Name);

            string path = EditorUtility.SaveFilePanelInProject(
                titleBuilder.ToString(),
                Path.GetFileName(defaultPath),
                "asset",
                messageBuilder.ToString(),
                defaultFolder
            );
            
            // Exit case - user cancelled
            if (string.IsNullOrEmpty(path)) return null;
            
            // Create the asset
            T newAsset = ScriptableObject.CreateInstance<T>();
            
            // Allow the caller to configure the asset
            if (setupAction != null)
            {
                SerializedObject serializedAsset = new SerializedObject(newAsset);
                setupAction.Invoke(newAsset, serializedAsset);
                serializedAsset.ApplyModifiedProperties();
            }
            
            // Save the asset
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Make Addressable if configured
            if (_assetConfigs.TryGetValue(typeof(T), out AssetTypeConfig config) && config.MakeAddressable)
            {
                MakeAddressable(path, config.AddressableLabel);
            }
            
            return newAsset;
        }

        /// <summary>
        /// Finds and retrieves all assets of the specified type that exist within the Unity project
        /// </summary>
        /// <typeparam name="T">The type of the asset to search for, which must inherit from ScriptableObject</typeparam>
        /// <returns>A list of found assets of the specified type, or an empty list if no assets are found</returns>
        public static List<T> FindAllAssets<T>() where T : ScriptableObject
        {
            List<T> results = new List<T>();
            
            // Build the guid
            StringBuilder guidBuilder = new StringBuilder();
            guidBuilder.Append("t:");
            guidBuilder.Append(typeof(T).Name);
            
            // Find all assets of the specified type
            string[] guids = AssetDatabase.FindAssets(guidBuilder.ToString());

            // Load each asset
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                // Skip if the asset is null
                if (!asset) continue;
                
                results.Add(asset);
            }

            return results;
        }

        /// <summary>
        /// Ensures that the specified folder path exists in the Unity AssetDatabase by creating any missing directories
        /// </summary>
        /// <param name="folderPath">The full path of the folder to check or create, using '/' as the path separator</param>
        private static void EnsureFolderExists(string folderPath)
        {
            // Exit case - the folder already exists
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            
            // Split path and create each folder level
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            StringBuilder pathBuilder = new StringBuilder();
            
            for (int i = 1; i < parts.Length; i++)
            {
                // Build the path
                pathBuilder.Clear();
                pathBuilder.Append(currentPath);
                pathBuilder.Append("/");
                pathBuilder.Append(parts[i]);
                string nextPath = pathBuilder.ToString();

                // Skip if the folder already exists
                if (AssetDatabase.IsValidFolder(nextPath))
                {
                    currentPath = nextPath;
                    continue;
                }
                
                // Create the folder
                AssetDatabase.CreateFolder(currentPath, parts[i]);
                currentPath = nextPath;
            }
        }

        /// <summary>
        /// Marks the specified asset at the given path as addressable, if the associated type configuration supports addressable assets
        /// </summary>
        /// <typeparam name="T">The type of the asset, which must inherit from ScriptableObject</typeparam>
        /// <param name="assetPath">The path of the asset to be marked as addressable</param>
        public static void MakeAssetAddressable<T>(string assetPath) where T : ScriptableObject
        {
            // Exit case - the asset was not found
            if (!_assetConfigs.TryGetValue(typeof(T), out AssetTypeConfig config)) return;

            // Exit case - the asset is not supposed to be an addressable
            if (!config.MakeAddressable) return;
            
            MakeAddressable(assetPath, config.AddressableLabel);
        }

        /// <summary>
        /// Marks a given asset as addressable and assigns a label if provided
        /// </summary>
        /// <param name="assetPath">The file path to the asset within the Unity project</param>
        /// <param name="label">The label to apply to the Addressable asset. Can be null or empty if no label is required</param>
        private static void MakeAddressable(string assetPath, string label)
        {
#if UNITY_EDITOR
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (!settings)
            {
                Debug.LogWarning("[AssetCreator] Addressables not configured. Asset will not be addressable.");
                return;
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            
            // Retrieve the asset
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            
            // Exit case - the guid doesn't exist
            if (string.IsNullOrEmpty(guid))
            {
                // Build the error message
                debugBuilder.Append("[AssetCreator] Could not find GUID for asset '");
                debugBuilder.Append(assetPath);
                debugBuilder.Append("'");
                
                Debug.LogError(debugBuilder.ToString());
                return;
            }
            
            // Check if already addressable
            AddressableAssetEntry existingEntry = settings.FindAssetEntry(guid);
            if (existingEntry != null)
            {
                // Exit case - no label was provided
                if (string.IsNullOrEmpty(label)) return;
                
                // Set the label for the existing entry
                EnsureLabelExists(settings, label);
                existingEntry.SetLabel(label, true);

                return;
            }
            
            // Add to the default group
            AddressableAssetGroup defaultGroup = settings.DefaultGroup;
            
            // Exit case - no default group exists
            if (!defaultGroup)
            {
                Debug.LogError("[AssetCreator] Default Addressable group not found.");
                return;
            }
            
            // Create the entry
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, defaultGroup);

            // Exit case - the entry failed to be created
            if (entry == null)
            {
                debugBuilder.Clear();
                debugBuilder.Append("[AssetCreator] Failed to create Addressable entry for asset '");
                debugBuilder.Append(assetPath);
                debugBuilder.Append("'");
                
                Debug.LogError(debugBuilder.ToString());
                return;
            }
            
            // Set address to asset name
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            entry.SetAddress(assetName);
            
            // Apply the label
            if (!string.IsNullOrEmpty(label))
            {
                EnsureLabelExists(settings, label);
                entry.SetLabel(label, true);
            }
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Ensures that a label exists within the specified Addressable Asset Settings
        /// </summary>
        /// <param name="settings">The AddressableAssetSettings instance where the label should be checked or added</param>
        /// <param name="label">The label to verify or create in the AddressableAssetSettings</param>
        private static void EnsureLabelExists(AddressableAssetSettings settings, string label)
        {
#if UNITY_EDITOR
            // Exit case - the label already exists
            if (settings.GetLabels().Contains(label)) return;
            
            // Create the label
            settings.AddLabel(label);
#endif
        }
    }
}