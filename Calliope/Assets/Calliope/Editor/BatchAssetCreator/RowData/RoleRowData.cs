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
        public override bool HasAnyData => !string.IsNullOrEmpty(ID) || !string.IsNullOrEmpty(DisplayName) || !string.IsNullOrEmpty(RequiredTraits) || !string.IsNullOrEmpty(ForbiddenTraits);

        /// <summary>
        /// Creates and returns a deep copy of the current instance
        /// </summary>
        /// <returns>A new instance of the corresponding class type that is a copy of the current instance</returns>
        public override BaseRowData Clone()
        {
            return new RoleRowData
            {
                ID = ID, 
                DisplayName = DisplayName, 
                RequiredTraits = RequiredTraits, 
                ForbiddenTraits = ForbiddenTraits
            };
        }
    }
}