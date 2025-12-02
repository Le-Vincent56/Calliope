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

        /// <summary>
        /// Creates and returns a deep copy of the current instance
        /// </summary>
        /// <returns>A new instance of <c>BaseRowData</c> that is a copy of the current instance</returns>
        public override BaseRowData Clone()
        {
            return new TraitRowData
            {
                ID = ID,
                DisplayName = DisplayName,
                CategoryIndex = CategoryIndex
            };
        }
    }
}