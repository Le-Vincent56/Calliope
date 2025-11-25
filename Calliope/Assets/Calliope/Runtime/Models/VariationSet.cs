using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a set of dialogue variations, each represented by a dialogue fragment;
    /// one of the variations can be selected based on certain conditions or preferences
    /// </summary>
    public class VariationSet : IVariationSet
    {
        public string ID { get; set;  }
        public string DisplayName { get; set; }
        public IReadOnlyList<IDialogueFragment> Variations { get; set; }
        
        public VariationSet()
        {
            Variations = new List<IDialogueFragment>();
        }
    }
}