using System.Text;
using Calliope.Unity.ScriptableObjects;

namespace Calliope.Editor.BatchAssetCreator.RowData.Conditions
{
    /// <summary>
    /// Represents data for a condition that evaluates whether a faction has a majority presence
    /// based on specified criteria in a batch asset creation workflow
    /// </summary>
    public class FactionMajorityConditionRowData : BaseConditionRowData
    {
        public FactionSO Faction = null;
        public int ComparisonTypeIndex = 0;
        public int Count = 1;

        public override bool IsValid => Faction;
        public override bool HasAnyData => Faction || Count != 1 || ComparisonTypeIndex != 0;

        /// <summary>
        /// Creates a deep copy of the current condition row data instance
        /// </summary>
        /// <returns>A new instance of <see cref="BaseConditionRowData"/> with the same properties as the current instance</returns>
        public override BaseConditionRowData Clone()
        {
            return new FactionMajorityConditionRowData
            {
                Faction = Faction,
                ComparisonTypeIndex = ComparisonTypeIndex,
                Count = Count
            };
        }

        /// <summary>
        /// Retrieves a unique identifier for the current row based on its properties
        /// </summary>
        /// <returns>A string representing the unique identifier for the row</returns>
        public override string GetRowID()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Faction_");
            builder.Append(Faction ? Faction.ID : "none");
            builder.Append("_");
            builder.Append(ComparisonTypeIndex);
            return builder.ToString();
        }
    }
}