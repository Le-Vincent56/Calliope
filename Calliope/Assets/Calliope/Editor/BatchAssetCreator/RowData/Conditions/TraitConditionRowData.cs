using System.Text;

namespace Calliope.Editor.BatchAssetCreator.RowData.Conditions
{
    /// <summary>
    /// Represents a condition row data type that associates a role with a specific trait requirement;
    /// includes properties to define the role, the trait, and whether the trait is mandatory
    /// </summary>
    public class TraitConditionRowData : BaseConditionRowData
    {
        public string RoleID = "";
        public string TraitID = "";
        public bool MustHaveTrait = true;

        public override bool IsValid => !string.IsNullOrEmpty(RoleID) && !string.IsNullOrEmpty(TraitID);
        public override bool HasAnyData => !string.IsNullOrEmpty(RoleID) || !string.IsNullOrEmpty(TraitID);

        /// <summary>
        /// Creates a new copy of the current TraitConditionRowData instance
        /// </summary>
        /// <returns>A new instance of <see cref="TraitConditionRowData"/> that replicates the current object's state</returns>
        public override BaseConditionRowData Clone()
        {
            return new TraitConditionRowData
            {
                RoleID = RoleID,
                TraitID = TraitID,
                MustHaveTrait = MustHaveTrait
            };
        }

        /// <summary>
        /// Concatenates the RoleID, TraitID, and MustHaveTrait properties to form a unique identifier for the row
        /// </summary>
        /// <returns>A string that represents the combination of RoleID, TraitID, and MustHaveTrait as a unique row identifier</returns>
        public override string GetRowID()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(RoleID);
            builder.Append("_");
            builder.Append(TraitID);
            builder.Append("_");
            builder.Append(MustHaveTrait);
            return builder.ToString();
        }
    }
}