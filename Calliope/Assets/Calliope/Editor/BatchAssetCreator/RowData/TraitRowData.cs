namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a trait
    /// </summary>
    public class TraitRowData : BaseRowData
    {
        public string ID = "";
        public string DisplayName = "";
        public int CategoryIndex = 0;
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(DisplayName);
        public override bool HasAnyData => !string.IsNullOrEmpty(ID) || !string.IsNullOrEmpty(DisplayName);
    }
}