using System.Collections.Generic;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of ITrait;
    /// allows traits to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Trait", menuName = "Calliope/Trait", order = 1)]
    public class TraitSO : ScriptableObject, ITrait
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this trait (e.g., 'brave', 'kind')")]
        [SerializeField] private string id;
        
        [Tooltip("Display name shown in UI (e.g., 'Brave', 'Kind')")]
        [SerializeField] private string displayName;
        
        [Header("Classification")]
        [Tooltip("The Category this trait belongs to")]
        [SerializeField] private TraitCategory category;
        
        [Header("Description")]
        [Tooltip("What this trait means and how it affects behavior")]
        [SerializeField] [TextArea(3, 5)] private string description;
        
        [Header("Conflicts")]
        [Tooltip("Traits that conflict with this one (e.g., 'brave' conflicts with 'cowardly')")]
        [SerializeField] private string[] conflictingTraitIDs;

        public string ID => id;
        public string DisplayName => displayName;
        public TraitCategory Category => category;
        public string Description => description;
        public IReadOnlyList<string> ConflictingTraitIDs => conflictingTraitIDs;

        private void OnValidate()
        {
            // Auto-generate ID from the display name if empty
            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(displayName))
                id = displayName.ToLower().Replace(" ", "-");
            
            // Ensure the display name is set (using the asset name as a fallback)
            if (string.IsNullOrEmpty(displayName))
                displayName = name;
        }
    }
}
