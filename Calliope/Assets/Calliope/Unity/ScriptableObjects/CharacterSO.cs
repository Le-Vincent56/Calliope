using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of ICharacter;
    /// allows characters to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "Calliope/Character", order = 2)]
    public class CharacterSO : ScriptableObject, ICharacter
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this trait (e.g., 'aldric', 'sera')")]
        [SerializeField] private string id;
        
        [Tooltip("Display name shown in UI (e.g., 'Aldric', 'Sera')")]
        [SerializeField] private string displayName;
        
        [Tooltip("Pronoun set for this character")]
        [SerializeField] private PronounSet pronouns = PronounSet.TheyThem;
        
        [Header("Traits")]
        [Tooltip("IDs of all the traits this character possesses")]
        [SerializeField] private string[] traitIDs;
        
        [Header("Faction")]
        [Tooltip("The faction this character belongs to (optional)")]
        [SerializeField] private FactionSO faction;
        
        [Header("Description")]
        [Tooltip("Character background and personality description")]
        [SerializeField] [TextArea(3, 5)] private string description;
        
        public string ID => id;
        public string DisplayName => displayName;
        public PronounSet Pronouns => pronouns;
        public IReadOnlyList<string> TraitIDs => traitIDs;
        public string FactionID => faction ? faction.ID : "";
        public string Description => description;

        /// <summary>
        /// Determines whether the character possesses the specified trait
        /// </summary>
        /// <param name="traitID">The unique identifier of the trait to check</param>
        /// <returns>True if the character possesses the specified trait; otherwise, false</returns>
        public bool HasTrait(string traitID)
        {
            for (int i = 0; i < traitIDs.Length; i++)
            {
                // Skip mismatching IDs
                if (traitIDs[i] != traitID) continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the character possesses any of the specified traits
        /// </summary>
        /// <param name="traitIDsToCheck">A collection of trait identifiers to check against the character's traits</param>
        /// <returns>True if the character possesses at least one of the specified traits; otherwise, false</returns>
        public bool HasAnyTrait(IEnumerable<string> traitIDsToCheck)
        {
            foreach (string traitID in traitIDsToCheck)
            {
                // Skip if the character doesn't possess the trait
                if (!HasTrait(traitID)) continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the character possesses all of the specified traits
        /// </summary>
        /// <param name="traitIDsToCheck">A collection of unique identifiers for the traits to check</param>
        /// <returns>True if the character possesses all of the specified traits; otherwise, false</returns>
        public bool HasAllTraits(IEnumerable<string> traitIDsToCheck)
        {
            foreach (string traitID in traitIDsToCheck)
            {
                // Skip if the character possesses the trait
                if (HasTrait(traitID)) continue;

                return false;
            }

            return true;
        }
        
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