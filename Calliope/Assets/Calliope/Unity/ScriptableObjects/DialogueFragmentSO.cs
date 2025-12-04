using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of IDialogueFragment;
    /// allows dialogue fragments to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue Fragment", menuName = "Calliope/Dialogue Fragment", order = 3)]
    public class DialogueFragmentSO : ScriptableObject, IDialogueFragment
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this dialogue fragment (e.g., 'proposal_aggressive_1')")]
        [SerializeField] private string id;
        
        [Header("Content")]
        [Tooltip("The dialogue text with variable placeholders (e.g., '{speaker.name}' glances at {target.name}")]
        [SerializeField] [TextArea(2, 10)] private string text;
        
        [Header("Scoring - Trait Affinities")]
        [Tooltip("The trait affinities affecting selection probability (e.g., aggressive +1.0 makes aggressive characters more likely to use this fragment)")]
        [SerializeField] private TraitAffinity[] traitAffinities;
        
        [Header("Scoring - Requirements")]
        [Tooltip("Traits the speaker must have to use this fragment")]
        [SerializeField] private string[] requiredTraitIDs;
        
        [Tooltip("Traits the speaker must not have to use this fragment")]
        [SerializeField] private string[] forbiddenTraitIDs;
        
        [Header("Scoring - Relationship Modifiers")]
        [Tooltip("How this fragment modifies relationships when spoken")]
        [SerializeField] private RelationshipModifier[] relationshipModifiers;
        
        [Header("Metadata")]
        [Tooltip("Tags for filtering and organization")]
        [SerializeField] private string[] tags;
        
        public string ID => id;
        public string Text => text;
        public IReadOnlyList<TraitAffinity> TraitAffinities => traitAffinities;
        public IReadOnlyList<string> RequiredTraitIDs => requiredTraitIDs;
        public IReadOnlyList<string> ForbiddenTraitIDs => forbiddenTraitIDs;
        public IReadOnlyList<RelationshipModifier> RelationshipModifiers => relationshipModifiers;
        public IReadOnlyList<string> Tags => tags;
    }
}