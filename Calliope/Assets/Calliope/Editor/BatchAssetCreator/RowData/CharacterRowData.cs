namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a character
    /// </summary>
    public class CharacterRowData : BaseRowData
    {
        public string ID = "";
        public string DisplayName = "";
        public int PronounIndex = 0;
        public string Traits = "";
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(DisplayName);
    }
}