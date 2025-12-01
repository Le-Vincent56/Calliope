namespace Calliope.Editor.BatchAssetCreator.Validation
{
    /// <summary>
    /// Represents a validation message with associated severity, content, and row index information
    /// </summary>
    public struct ValidationMessage
    {
        public ValidationSeverity Severity;
        public string Message;
        public int RowIndex;    // -1 for global message, 0+ for row-specific
    }
}