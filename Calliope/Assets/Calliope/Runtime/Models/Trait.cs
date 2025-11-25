using System.Collections.Generic;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a character trait that defines specific attributes or personality
    /// aspects of an entity; traits are categorized and can influence behaviors
    /// or interactions within the system
    /// </summary>
    public class Trait : ITrait
    {
        public string ID { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public TraitCategory Category { get; set; }
        public IReadOnlyList<string> ConflictingTraitIDs { get; set; }

        public Trait()
        {
            ConflictingTraitIDs = new List<string>();
        }
    }
}