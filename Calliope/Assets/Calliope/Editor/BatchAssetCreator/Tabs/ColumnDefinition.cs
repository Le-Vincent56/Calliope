namespace Calliope.Editor.BatchAssetCreator.Tabs
{
    /// <summary>
    /// Represents the definition of a column, including its header, width, flexible growth setting, and tooltip
    /// </summary>
    public struct ColumnDefinition
    {
        public string Header;
        public float? Width;        // Fixed width, or null for flexible
        public float FlexGrow;      // Used if Width is null
        public string Tooltip;

        public ColumnDefinition(string header, float? width = null, float flexGrow = 0, string tooltip = null)
        {
            Header = header;
            Width = width;
            FlexGrow = flexGrow;
            Tooltip = tooltip;
        }
    }
}