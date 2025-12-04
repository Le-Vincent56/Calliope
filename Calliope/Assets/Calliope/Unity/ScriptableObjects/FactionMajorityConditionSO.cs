using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Faction Majority Condition", menuName = "Calliope/Branch Condition/Faction Majority Condition", order = 14)]
    public class FactionMajorityConditionSO : BranchConditionSO
    {
        [Header("Faction")]
        [Tooltip("The faction to evaluate")]
        [SerializeField] private FactionSO targetFaction;

        [Header("Comparison")]
        [Tooltip("How to compare the faction count")]
        [SerializeField] private FactionComparisonType comparisonType = FactionComparisonType.Majority;

        [Tooltip("The count to compare against (used with AtLeast, Exactly, MoreThan, LessThan)")]
        [SerializeField] [Min(0)] private int count = 1;
        
        /// <summary>
        /// Evaluates the faction majority condition within the provided scene context, based on the distribution
        /// of characters across factions and a specified comparison type.
        /// </summary>
        /// <param name="cast">A read-only dictionary of characters keyed by their identifiers, used to determine faction membership and counts</param>
        /// <param name="relationships">An interface providing details about relationships between characters that may influence the evaluation</param>
        /// <param name="sceneContext">The current scene context containing relevant environmental or state data for evaluation; can be null</param>
        /// <returns>
        /// A boolean indicating whether the specified faction condition is met based on the provided comparison type
        /// </returns>
        public override bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships, ISceneContext sceneContext = null)
        {
            // Exit case - invalid parameters
            if (cast == null || cast.Count == 0 || !targetFaction) 
                return false;

            string targetFactionID = targetFaction.ID;
            
            // Count the members of each faction
            Dictionary<string, int> factionCounts = new Dictionary<string, int>();

            foreach (KeyValuePair<string, ICharacter> kvp in cast)
            {
                // Skip if the character does not exist
                if (kvp.Value == null) continue;

                string factionID = kvp.Value.FactionID ?? "";

                // Count the faction
                if (!factionCounts.TryAdd(factionID, 1))
                    factionCounts[factionID]++;
            }

            int targetCount = 0;
            if (factionCounts.TryGetValue(targetFactionID, out int factionCount))
                targetCount = factionCount;

            switch (comparisonType)
            {
                case FactionComparisonType.Majority:
                    return IsMajority(factionCounts, targetFactionID, targetCount);
                
                case FactionComparisonType.Minority:
                    return IsMinority(factionCounts, targetFactionID, targetCount);
                
                case FactionComparisonType.AtLeast:
                    return targetCount >= count;
                
                case FactionComparisonType.Exactly:
                    return targetCount == count;
                
                case FactionComparisonType.MoreThan:
                    return targetCount > count;
                
                case FactionComparisonType.LessThan:
                    return targetCount < count;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Generates a descriptive string summarizing the faction condition represented by the combination of the
        /// target faction identifier and the specified comparison type; the description specifies how the target faction
        /// relates to its members' count or majority status
        /// </summary>
        /// <returns>
        /// A string describing the faction condition, including the target faction and the applied comparison criteria
        /// </returns>
        public override string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Faction '");
            builder.Append(targetFaction.ID);
            builder.Append("' ");

            switch (comparisonType)
            {
                case FactionComparisonType.Majority:
                    builder.Append("is majority");
                    break;
                
                case FactionComparisonType.Minority:
                    builder.Append("is minority");
                    break;
                
                case FactionComparisonType.AtLeast:
                    builder.Append("has at least ");
                    builder.Append(count);
                    builder.Append(" members");
                    break;
                
                case FactionComparisonType.Exactly:
                    builder.Append("has exactly ");
                    builder.Append(count);
                    builder.Append(" members");
                    break;
                
                case FactionComparisonType.MoreThan:
                    builder.Append("has more than ");
                    builder.Append(count);
                    builder.Append(" members");
                    break;
                
                case FactionComparisonType.LessThan:
                    builder.Append("has fewer than ");
                    builder.Append(count);
                    builder.Append(" members");
                    break;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Determines if the target faction has a majority compared to other factions in the given counts
        /// </summary>
        /// <param name="counts">A dictionary containing the number of members in each faction, keyed by faction identifier</param>
        /// <param name="target">The identifier of the target faction to evaluate for majority status</param>
        /// <param name="targetCount">The member count of the target faction</param>
        /// <returns>
        /// A boolean indicating whether the target faction's member count is strictly greater than the counts of all other factions
        /// </returns>
        private bool IsMajority(Dictionary<string, int> counts, string target, int targetCount)
        {
            // Exit case - no targets
            if (targetCount == 0) return false;

            foreach (KeyValuePair<string, int> kvp in counts)
            {
                // Skip if the key equals the target
                if (kvp.Key == target) continue;
                
                // Skip if the value is greater than the target count
                if (kvp.Value >= targetCount) return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the target faction is in the minority compared to all other factions
        /// based on the provided counts.
        /// </summary>
        /// <param name="counts">A dictionary containing the counts of members for each faction, keyed by faction ID</param>
        /// <param name="target">The ID of the target faction being evaluated</param>
        /// <param name="targetCount">The member count of the target faction</param>
        /// <returns>
        /// A boolean indicating whether the target faction is in the minority compared to all other factions
        /// </returns>
        private bool IsMinority(Dictionary<string, int> counts, string target, int targetCount)
        {
            // Exit case - target count is 0 (always in the minority if there are 0 members)
            if (targetCount == 0) return true;

            foreach (KeyValuePair<string, int> kvp in counts)
            {
                // Skip if it is the target being checked
                if (kvp.Key == target) continue;
                
                // Exit case - the value is less than or equal to the target count
                if(kvp.Value <= targetCount) return false;
            }

            return true;
        }
    }
}