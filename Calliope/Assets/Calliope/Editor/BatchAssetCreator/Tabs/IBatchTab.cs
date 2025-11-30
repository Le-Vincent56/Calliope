using System;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Defines the contract for implementing a tab interface for batch asset creation,
    /// supporting the management of rows, user interaction, and asset generation functionality
    /// </summary>
    public interface IBatchTab
    {
        /// <summary>
        /// The name of the tab to display
        /// </summary>
        string TabName { get; }
        
        /// <summary>
        /// The number of valid rows in the tab, (i.e., rows that contain valid data)
        /// </summary>
        int ValidRowCount { get; }

        /// <summary>
        /// Constructs the user interface content for the tab, including a container for rows,
        /// alongside UI components for managing rows, such as an "Add Row" button
        /// </summary>
        /// <param name="onRowsChanged">The action to execute when the collection of rows changes in response to user interaction</param>
        /// <returns>A VisualElement representing the constructed UI layout of the tab</returns>
        VisualElement BuildContent(Action onRowsChanged);

        /// <summary>
        /// Generates assets based on the provided base folder path, while utilizing the data
        /// from the current set of rows in the tab
        /// </summary>
        /// <param name="baseFolderPath">The path to the base folder where the assets should be created</param>
        /// <returns>The number of valid assets successfully created</returns>
        int CreateAssets(string baseFolderPath);

        /// <summary>
        /// Removes all rows from the current tab and clears the associated data structure,
        /// resetting it to its initial state while updating the UI and triggering necessary callbacks
        /// </summary>
        void ClearRows();
    }
}