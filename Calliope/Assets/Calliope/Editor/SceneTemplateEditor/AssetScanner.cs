using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Utility class for scanning the ScriptableObject assets in the project;
    /// based on the current scan mode
    /// </summary>
    public static class AssetScanner
    {
        private const string ScanModePrefKey = "Calliope_AssetScanMode";
        private static AssetScanMode _currentMode = AssetScanMode.All;
        private static bool _modeLoaded = false;

        public static AssetScanMode CurrentMode
        {
            get
            {
                if (!_modeLoaded)
                {
                    _currentMode = (AssetScanMode)EditorPrefs.GetInt(ScanModePrefKey, (int)AssetScanMode.All);
                    _modeLoaded = true;
                }
                return _currentMode;
            }
            set
            {
                _currentMode = value;
                EditorPrefs.SetInt(ScanModePrefKey, (int)value);
            }
        }

        /// <summary>
        /// Finds and returns a list of ScriptableObject assets of the specified type present in the project
        /// </summary>
        /// <typeparam name="T">The type of ScriptableObject assets to find</typeparam>
        /// <returns>A list of ScriptableObject assets of the specified type found in the project</returns>
        public static List<T> FindAssets<T>() where T : ScriptableObject
        {
            List<T> results = new List<T>();
            
            // Get the asset GUIDs
            string typeName = typeof(T).Name;
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}");

            for (int i = 0; i < guids.Length; i++)
            {
                // Get the path of the asset
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // Filter based on mode
                if (!PassesModeFilter(path)) continue;

                // Load the asset
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                
                // Skip if the asset is null
                if (!asset) continue;
                
                results.Add(asset);
            }

            return results;
        }

        /// <summary>
        /// Finds and returns a list of ScriptableObject assets of the specified type located within the given search path
        /// </summary>
        /// <param name="searchPath">The path to search for assets of the specified type</param>
        /// <typeparam name="T">The type of ScriptableObject assets to find</typeparam>
        /// <returns>A list of ScriptableObject assets of the specified type found within the given search path</returns>
        public static List<T> FindAssetsInPath<T>(string searchPath) where T : ScriptableObject
        {
            List<T> results = new List<T>();

            // Get the asset GUIDs
            string typeName = typeof(T).Name;
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { searchPath });

            for (int i = 0; i < guids.Length; i++)
            {
                // Get the path of the asset
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // Filter based on mode
                if (!PassesModeFilter(path)) continue;

                // Load the asset
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                
                // Skip if the asset is null
                if (!asset) continue;
                
                results.Add(asset);
            }

            return results;
        }

        /// <summary>
        /// Determines whether the given asset path satisfies the current scan mode filter
        /// </summary>
        /// <param name="path">The file path of the asset to evaluate</param>
        /// <returns>True if the asset path passes the filter criteria defined by the current scan mode; otherwise, false</returns>
        private static bool PassesModeFilter(string path)
        {
            return CurrentMode switch
            {
                AssetScanMode.All => true,
                AssetScanMode.Resources => IsInResourcesFolder(path),
                AssetScanMode.Addressables => IsAddressable(path),
                _ => true
            };
        }

        /// <summary>
        /// Determines whether the specified asset path is located within a Resources folder in the project
        /// </summary>
        /// <param name="path">The file path of the asset to evaluate</param>
        /// <returns>True if the asset path is located inside a Resources folder; otherwise, false</returns>
        private static bool IsInResourcesFolder(string path)
        {
            return path.Contains("/Resources/") || path.Contains("\\Resources\\");
        }

        /// <summary>
        /// Determines whether the given asset path corresponds to an Addressable asset
        /// </summary>
        /// <param name="path">The file path of the asset to evaluate</param>
        /// <returns>True if the asset is addressable; otherwise, false</returns>
        private static bool IsAddressable(string path)
        {
            try
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings)
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    return entry != null;
                }
            }
            catch
            {
                // Addressables not properly configured
            }

            // Fallback, check if an Addressables folder exists
            return path.Contains("/Addressables/") || path.Contains("\\Addressables\\");
        }

        /// <summary>
        /// Returns the display name for a given <see cref="AssetScanMode"/>
        /// </summary>
        /// <param name="mode">The AssetScanMode enumeration value whose display name is to be retrieved</param>
        /// <returns>A string representing the display name of the specified mode</returns>
        public static string GetModeDisplayName(AssetScanMode mode)
        {
            return mode switch
            {
                AssetScanMode.All => "All",
                AssetScanMode.Resources => "Resources Only",
                AssetScanMode.Addressables => "Addressables Only",
                _ => mode.ToString()
            };
        }
    }
}