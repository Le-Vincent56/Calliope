namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a dialogue fragment asset
    /// </summary>
    public class FragmentRowData : BaseRowData
    {
        public string ID = "";
        public string Text = "";
        public string TraitAffinities = ""; // Format: "brave:1.5, kind:1.0"
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(Text);
    }
}