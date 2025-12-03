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

        public string ToRoleID = "";
        public int RelationshipTypeIndex = 0;
        public float Threshold = 50f;
        public bool GreaterThanOrEqual = true;
        
        public override bool IsValid => !string.IsNullOrEmpty(RoleID);
        public override bool HasAnyData => !string.IsNullOrEmpty(RoleID) || !string.IsNullOrEmpty(TraitID);

        /// <summary>
        /// Creates and returns a deep copy of the current <c>ConditionRowData</c> instance
        /// </summary>
        /// <returns>A new instance of <c>ConditionRowData</c> that is a copy of the current instance</returns>
        public override BaseRowData Clone()
        {
            return new ConditionRowData
            {
                ConditionType = ConditionType,
                RoleID = RoleID,
                TraitID = TraitID,
                MustHaveTrait = MustHaveTrait
            };
        }
    }
}