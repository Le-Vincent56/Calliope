using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a fragment of dialogue that can be used in various scenarios;
    /// contains the text of the dialogue and metadata that influences its selection
    /// </summary>
    public class DialogueFragment : IDialogueFragment
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public IReadOnlyList<TraitAffinity> TraitAffinities { get; set; }
        public IReadOnlyList<RelationshipModifier> RelationshipModifiers { get; set; }
        public IReadOnlyList<string> RequiredTraitIDs { get; set; }
        public IReadOnlyList<string> ForbiddenTraitIDs { get; set; }
        public IReadOnlyList<string> Tags { get; set; }

        public DialogueFragment()
        {
            TraitAffinities = new List<TraitAffinity>();
            RelationshipModifiers = new List<RelationshipModifier>();
            RequiredTraitIDs = new List<string>();
            ForbiddenTraitIDs = new List<string>();
            Tags = new List<string>();
        }
    }
}