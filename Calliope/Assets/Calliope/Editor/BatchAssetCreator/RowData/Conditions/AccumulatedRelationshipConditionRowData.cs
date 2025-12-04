using System.Collections.Generic;
using System.Text;
using Calliope.Core.ValueObjects;

namespace Calliope.Editor.BatchAssetCreator.RowData.Conditions
{
    /// <summary>
    /// Represents data for a condition that evaluates accumulated relationships in a batch asset creation context
    /// </summary>
    public class AccumulatedRelationshipConditionRowData : BaseConditionRowData
    {
        public List<RolePair> RolePairs = new List<RolePair>();
        public int AggregationTypeIndex = 0;
        public int RelationshipTypeIndex = 0;
        public float Threshold = 50f;
        public bool GreaterThanOrEqual = true;
        
        public override bool IsValid => RolePairs is { Count: > 0 };
        public override bool HasAnyData => RolePairs is { Count: > 0 };

        /// <summary>
        /// Creates a new copy of the current <see cref="AccumulatedRelationshipConditionRowData"/> instance
        /// </summary>
        /// <returns>A new <see cref="BaseConditionRowData"/> instance that is a clone of the current object</returns>
        public override BaseConditionRowData Clone()
        {
            AccumulatedRelationshipConditionRowData clone = new AccumulatedRelationshipConditionRowData()
            {
                AggregationTypeIndex = AggregationTypeIndex,
                RelationshipTypeIndex = RelationshipTypeIndex,
                Threshold = Threshold,
                GreaterThanOrEqual = GreaterThanOrEqual
            };
            
            // Copy over the role pairs
            for (int i = 0; i < RolePairs.Count; i++)
            {
                clone.RolePairs.Add(new RolePair(RolePairs[i].FromRoleID, RolePairs[i].ToRoleID));
            }

            return clone;
        }

        /// <summary>
        /// Generates a unique identifier string for the current <see cref="AccumulatedRelationshipConditionRowData"/> instance
        /// </summary>
        /// <returns>A string representing the unique identifier for the row, incorporating the aggregation type and the number of role pairs</returns>
        public override string GetRowID()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("AccRel_");
            builder.Append(AggregationTypeIndex);
            builder.Append("aggr_");
            builder.Append(RelationshipTypeIndex);
            builder.Append("rel_");
            builder.Append(RolePairs.Count);
            builder.Append("pairs");
            return builder.ToString();
        }
    }
}
