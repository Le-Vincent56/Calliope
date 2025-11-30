using Calliope.Unity.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Watches for asset changes and notifies the Scene Template Editor to
    /// refresh validation
    /// </summary>
    public class SceneTemplateAssetWatcher : AssetPostprocessor
    {
        /// <summary>
        /// Processes assets after they are imported, deleted, or moved, and triggers
        /// a refresh for all open Scene Template Editor windows if any relevant assets are affected
        /// </summary>
        /// <param name="importedAssets">An array of paths to assets that have been imported or updated</param>
        /// <param name="deletedAssets">An array of paths to assets that have been deleted</param>
        /// <param name="movedAssets">An array of paths to assets that have been moved to a new location</param>
        /// <param name="movedFromAssetPaths">An array of original paths for the assets that have been moved</param>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if any relevant assets were changed
            bool shouldRefresh = false;

            // Check deleted assets
            for (int i = 0; i < deletedAssets.Length; i++)
            {
                if (IsRelevantAsset(deletedAssets[i]))
                {
                    shouldRefresh = true;
                    break;
                }
            }

            // Check imported/modified assets
            if (!shouldRefresh)
            {
                for (int i = 0; i < importedAssets.Length;
                     i++)
                {
                    if (IsRelevantAsset(importedAssets[i]))
                    {
                        shouldRefresh = true;
                        break;
                    }
                }
            }

            // Check moved assets
            if (!shouldRefresh)
            {
                for (int i = 0; i < movedAssets.Length;
                     i++)
                {
                    if (IsRelevantAsset(movedAssets[i]))
                    {
                        shouldRefresh = true;
                        break;
                    }
                }
            }

            if (shouldRefresh)
            {
                // Notify all open Scene Template Editorwindows to refresh
                SceneTemplateEditorWindow[] windows = Resources.FindObjectsOfTypeAll<SceneTemplateEditorWindow>();
                for (int i = 0; i < windows.Length; i++)
                {
                    windows[i].OnExternalAssetChange();
                }
            }
        }

        /// <summary>
        /// Determines whether a given asset is relevant based on its type;
        /// an asset is considered relevant if it matches specific ScriptableObject types
        /// such as SceneRoleSO, VariationSetSO, SceneBeatSO, or SceneTemplateSO
        /// </summary>
        /// <param name="assetPath">The file path of the asset to evaluate</param>
        /// <returns>True if the asset is relevant; otherwise, false</returns>
        private static bool IsRelevantAsset(string assetPath)
        {
            // Exit case - not an asset
            if (!assetPath.EndsWith(".asset")) return false;
            
            // Load the asset type without fully loading the asset
            System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            // Exit case - the asset type is null
            if (assetType == null) return true;

            // Check if it's a type we care about
            if (typeof(SceneRoleSO).IsAssignableFrom(assetType)) 
                return true;
            if (typeof(VariationSetSO).IsAssignableFrom(assetType))
                return true;
            if (typeof(SceneBeatSO).IsAssignableFrom(assetType)) 
                return true;
            if (typeof(SceneTemplateSO).IsAssignableFrom(assetType))
                return true;

            return false;
        }
    }
}