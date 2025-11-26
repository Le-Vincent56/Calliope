using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of IBeatBranch;
    /// allows beat branches to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Beat Branch", menuName = "Calliope/Beat Branch", order = 7)]
    public class BeatBranchSO : ScriptableObject, IBeatBranch
    {
        [Header("Target")]
        [Tooltip("ID of the beat to transition to if conditions are met")]
        [SerializeField] private string nextBeatID;
        
        [Header("Conditions")]
        [Tooltip("All conditions that must be true for this branch to be taken; Empty means this will always succeed")]
        [SerializeField] private BranchConditionSO[] conditions;
        
        public string NextBeatID => nextBeatID;
        public IReadOnlyList<IBranchCondition> Conditions => conditions;

        private void OnValidate()
        {
            StringBuilder warningBuilder = new StringBuilder();
            
            // Validate target beat ID
            if (string.IsNullOrEmpty(nextBeatID))
            {
                warningBuilder.Append("[BeatBranchSO] '");
                warningBuilder.Append(name);
                warningBuilder.Append("' has no target beat ID");
                
                Debug.LogWarning(warningBuilder.ToString(), this);
            }
            
            // Exit case - no conditions were provided
            if (conditions == null || conditions.Length <= 0) return;
            
            // Validate conditions
            int nullCount = 0;
            for (int i = 0; i < conditions.Length; i++)
            {
                // Skip over valid conditions
                if (conditions[i] != null) continue;

                nullCount++;
            }

            // Exit case - no null conditions
            if (nullCount <= 0) return;
                
            warningBuilder.Clear();
            warningBuilder.Append("[BeatBranchSO] '");
            warningBuilder.Append(name);
            warningBuilder.Append("' has ");
            warningBuilder.Append(nullCount);
            warningBuilder.Append(" null conditions. Assign BranchConditionSO assets");
                    
            Debug.LogWarning(warningBuilder.ToString(), this);
        }
    }
}