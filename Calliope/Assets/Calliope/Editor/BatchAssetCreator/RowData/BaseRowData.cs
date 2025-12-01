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
    }
}
