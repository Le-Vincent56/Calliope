using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of ISceneRole;
    /// allows scene roles to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Scene Role", menuName = "Calliope/Scene Role", order = 5)]
    public class SceneRoleSO : ScriptableObject, ISceneRole
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this role (e.g., 'instigator', 'objector')")]
        [SerializeField] private string roleID;
        
        [Tooltip("Display name for editor organization (e.g., 'Instigator', 'Objector')")]
        [SerializeField] private string displayName;
        
        [Header("Casting - Preferences")]
        [Tooltip("Traits that increase casting score (soft preferences")]
        [SerializeField] private TraitAffinity[] preferredTraits;
        
        [Header("Casting - Requirements")]
        [Tooltip("Traits that must be present to cast (hard requirements)")]
        [SerializeField] private string[] requiredTraitIDs;
        
        [Tooltip("Traits that must not be present to cast (hard restrictions)")]
        [SerializeField] private string[] forbiddenTraitIDs;
        
        public string RoleID => roleID;
        public string DisplayName => displayName;
        public IReadOnlyList<TraitAffinity> PreferredTraits => preferredTraits;
        public IReadOnlyList<string> RequiredTraitIDs => requiredTraitIDs;
        public IReadOnlyList<string> ForbiddenTraitIDs => forbiddenTraitIDs;
    }
}