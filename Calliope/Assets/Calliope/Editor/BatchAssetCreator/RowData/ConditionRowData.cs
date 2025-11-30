namespace Calliope.Editor.BatchAssetCreator.RowData
{
    /// <summary>
    /// Data for a condition
    /// </summary>
    public class ConditionRowData : BaseRowData
    {
        public int ConditionType = 0;
        public string RoleID = "";
        public string TraitID = "";
        public bool MustHaveTrait = true;
        
        public override bool IsValid => !string.IsNullOrEmpty(RoleID);
    }
}