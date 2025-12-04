using Calliope.Unity.ScriptableObjects;

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
        public FactionSO Faction = null;
        
        public override bool IsValid => !string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(DisplayName);
        public override bool HasAnyData => !string.IsNullOrEmpty(ID) || !string.IsNullOrEmpty(DisplayName) || !string.IsNullOrEmpty(Traits) || Faction;

        /// <summary>
        /// Creates and returns a deep copy of the current <c>CharacterRowData</c> instance
        /// </summary>
        /// <returns>A new instance of <c>CharacterRowData</c> that is a copy of the current instance</returns>
        public override BaseRowData Clone()
        {
            return new CharacterRowData
            {
                ID = ID,
                DisplayName = DisplayName,
                PronounIndex = PronounIndex,
                Traits = Traits,
                Faction = Faction
            };
        }
    }
}