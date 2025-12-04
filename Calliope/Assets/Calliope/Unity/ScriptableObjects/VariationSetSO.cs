using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of IVariationSet;
    /// allows variation sets to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Variation Set", menuName = "Calliope/Variation Set", order = 4)]
    public class VariationSetSO : ScriptableObject, IVariationSet
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this variation set (e.g., 'proposal_aggressive')")]
        [SerializeField] private string id;
        
        [Tooltip("Display name for editor organization (e.g., 'Aggressive Proposal')")]
        [SerializeField] private string displayName;
        
        [Header("Variations")]
        [Tooltip("All dialogue fragment variations for this moment")]
        [SerializeField] private DialogueFragmentSO[] variations;
        
        [Header("Metadata")]
        [Tooltip("Description of what this moment represents")]
        [SerializeField] [TextArea(2, 4)] private string description;
        
        [Tooltip("Tags for filtering and organization")]
        [SerializeField] private string[] tags;
        
        public string ID => id;
        public string DisplayName => displayName;
        public IReadOnlyList<IDialogueFragment> Variations => variations;
        public string Description => description;
        public IReadOnlyList<string> Tags => tags;
    }
}