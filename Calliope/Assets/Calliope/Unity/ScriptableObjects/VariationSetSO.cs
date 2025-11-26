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

        private void OnValidate()
        {
            // Auto-generate ID from the display name if empty
            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(displayName))
                id = displayName.ToLower().Replace(" ", "-");
            
            // Ensure the display name is set (using the asset name as a fallback)
            if (string.IsNullOrEmpty(displayName))
                displayName = name;
            
            // Validate variations
            if (variations is { Length: > 0 })
            {
                // Count nulls
                int nullCount = 0;
                for (int i = 0; i < variations.Length; i++)
                {
                    // Skip over valid variations
                    if (variations[i]) continue;
                    
                    nullCount++;
                }

                // Exit case - no null variations
                if (nullCount <= 0) return;
                
                // Log warning
                StringBuilder warningBuilder = new StringBuilder();
                warningBuilder.Append("[VariationSetSO] ");
                warningBuilder.Append(name);
                warningBuilder.Append(" has ");
                warningBuilder.Append(nullCount);
                warningBuilder.Append(" null variation(s). Assign DialogueFragmentSO assets");
                Debug.LogWarning(warningBuilder.ToString(), this);
            }
        }
    }
}