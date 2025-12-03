using System;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Tabs;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    public interface IConditionRowBuilder
    {
        string DisplayName { get; }
        ColumnDefinition[] Columns { get; }
        string AssetTypeName { get; }

        /// <summary>
        /// Creates and returns a new instance of condition row data specific to the implemented builder
        /// </summary>
        /// <returns>
        /// A newly instantiated <see cref="BaseConditionRowData"/>, representing condition row data
        /// that matches the builder type
        /// </returns>
        BaseConditionRowData CreateRowData();

        /// <summary>
        /// Constructs and populates the row fields in the provided container using the given data
        /// and notifies when the data is changed
        /// </summary>
        /// <param name="container">The visual container where the row fields will be added</param>
        /// <param name="data">The data used to populate the row fields</param>
        /// <param name="onDataChanged">An action that is invoked whenever the data is modified</param>
        void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged);

        /// <summary>
        /// Creates a new asset based on the provided data and saves it to the specified folder path
        /// </summary>
        /// <param name="data">The data used to define the asset being created</param>
        /// <param name="folderPath">The file system path where the new asset will be saved</param>
        /// <returns>A boolean indicating whether the asset creation and saving were successful</returns>
        bool CreateAsset(BaseConditionRowData data, string folderPath);
    }
}
