namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a variation set
    /// </summary>
    public class VariationSetRowData : BaseRowData
    {
        public string ID = "";
        public string DisplayName = "";
        public string Description = "";
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(DisplayName);
    }
}