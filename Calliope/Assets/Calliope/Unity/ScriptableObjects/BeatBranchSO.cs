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
    }
}