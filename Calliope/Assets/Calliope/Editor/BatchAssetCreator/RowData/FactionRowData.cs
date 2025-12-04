namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a faction
    /// </summary>
    public class FactionRowData : BaseRowData
    {
        public string ID = "";
        public string DisplayName = "";
        public string Description = "";
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(DisplayName);
        public override bool HasAnyData => !string.IsNullOrEmpty(ID) || !string.IsNullOrEmpty(DisplayName) || !string.IsNullOrEmpty(Description);

        /// <summary>
        /// Creates and returns a deep copy of the current instance
        /// </summary>
        /// <returns>A new instance of the corresponding class type that is a copy of the current instance</returns>
        public override BaseRowData Clone()
        {
            return new FactionRowData
            {
                ID = ID,
                DisplayName = DisplayName,
                Description = Description
            };
        }
    }
}