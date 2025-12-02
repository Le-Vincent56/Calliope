namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Base class for all batch creation row data
    /// </summary>
    public abstract class BaseRowData
    {
        /// <summary>
        /// Returns true if this row has enough data to create an asset
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// Determines whether this row contains any populated data fields
        /// </summary>
        public abstract bool HasAnyData { get; }

        /// <summary>
        /// Creates and returns a deep copy of the current <c>BaseRowData</c> instance
        /// </summary>
        /// <returns>A new instance of <c>BaseRowData</c> that is a copy of the current instance</returns>
        public abstract BaseRowData Clone();
    }
}
