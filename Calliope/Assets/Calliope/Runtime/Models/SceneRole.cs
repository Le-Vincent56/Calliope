using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a role that characters can assume within a scene;
    /// provides information about the role's unique identifier, display name, and
    /// its associations with traits such as preferred, required, and forbidden traits
    /// </summary>
    public class SceneRole : ISceneRole
    {
        public string RoleID { get; set;  }
        public string DisplayName { get; set; }
        public IReadOnlyList<TraitAffinity> PreferredTraits { get; set;  }
        public IReadOnlyList<string> RequiredTraitIDs { get; set;  }
        public IReadOnlyList<string> ForbiddenTraitIDs { get; set;  }

        public SceneRole()
        {
            PreferredTraits = new List<TraitAffinity>();
            RequiredTraitIDs = new List<string>();
            ForbiddenTraitIDs = new List<string>();
        }
    }
}