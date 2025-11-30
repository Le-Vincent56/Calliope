namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a role
    /// </summary>
    public class RoleRowData : BaseRowData
    {
        public string ID = "";
        public string DisplayName = "";
        public string RequiredTraits = "";      // Comma-separated
        public string ForbiddenTraits = "";     // Comma-separated
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(DisplayName);
    }
}