using System.Text;

namespace Calliope.Editor.BatchAssetCreator.RowData.Conditions
{
    /// <summary>
    /// Represents a condition row data type that defines parameters and thresholds for relationships between roles;
    /// includes properties for identifying the roles involved, the type of relationship, and comparison criteria
    /// </summary>
    public class RelationshipConditionRowData : BaseConditionRowData
    {
        public string FromRoleID = "";
        public string ToRoleID = "";
        public int RelationshipTypeIndex = 0;
        public float Threshold = 50f;
        public bool GreaterThanOrEqual = true;
        
        public override bool IsValid => !string.IsNullOrEmpty(FromRoleID) && !string.IsNullOrEmpty(ToRoleID);
        public override bool HasAnyData => !string.IsNullOrEmpty(FromRoleID) || !string.IsNullOrEmpty(ToRoleID);

        /// <summary>
        /// Creates a new copy of the current object instance, preserving its state
        /// </summary>
        /// <returns>A new instance of <see cref="BaseConditionRowData"/> that is a duplicate of the current object</returns>
        public override BaseConditionRowData Clone()
        {
            return new RelationshipConditionRowData
            {
                FromRoleID = FromRoleID,
                ToRoleID = ToRoleID,
                RelationshipTypeIndex = RelationshipTypeIndex,
                Threshold = Threshold,
                GreaterThanOrEqual = GreaterThanOrEqual
            };
        }

        /// <summary>
        /// Retrieves a unique identifier for the current row data object based on its properties
        /// </summary>
        /// <returns>A string representing the unique identifier for this row data</returns>
        public override string GetRowID()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FromRoleID);
            builder.Append("_");
            builder.Append(ToRoleID);
            builder.Append("_");
            builder.Append(GreaterThanOrEqual ? "GTE" : "LTE");
            builder.Append("_");
            builder.Append((int)Threshold);
            return builder.ToString();
        }
    }
}