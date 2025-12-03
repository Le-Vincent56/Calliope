using UnityEngine;

namespace Calliope.Editor.BatchAssetCreator.RowData.Conditions
{
    /// <summary>
    /// Base class for all condition row data types
    /// </summary>
    public abstract class BaseConditionRowData
    {
        /// <summary>
        /// Gets a value indicating whether the current condition row contains valid configuration or state
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// Gets a value indicating whether the condition row contains any meaningful or valid data
        /// </summary>
        public abstract bool HasAnyData { get; }

        /// <summary>
        /// Creates a new copy of the current condition row data
        /// </summary>
        /// <returns>A new instance of <see cref="BaseConditionRowData"/> that is a clone of the current object</returns>
        public abstract BaseConditionRowData Clone();

        /// <summary>
        /// Retrieves the identifier associated with the row
        /// </summary>
        /// <returns>A string that represents the unique identifier of the row</returns>
        public abstract string GetRowID();
    }
}
