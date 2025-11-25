using System.Collections.Generic;
using Calliope.Core.ValueObjects;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A role that characters can be cast into for a scene;
    /// Example: "Instigator" role prefers aggressive and brave characters
    /// </summary>
    public interface ISceneRole
    {
        /// <summary>
        /// Unique identifier ("instigator", "objector")
        /// </summary>
        string RoleID { get; }
        
        /// <summary>
        /// Display name ("Instigator", "Objector")
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Traits that increase the casting score;
        /// Example: aggressive +1.0, brave +1.0
        /// </summary>
        IReadOnlyList<TraitAffinity> PreferredTraits { get; }
        
        /// <summary>
        /// Traits that must be present for this role to be cast
        /// Example: ["leader"] means only leaders can fill this role
        /// </summary>
        IReadOnlyList<string> RequiredTraitIDs { get; }
        
        /// <summary>
        /// Traits that prevent this role from being cast;
        /// Example: ["timid"] means timid characters cannot fill this role
        /// </summary>
        IReadOnlyList<string> ForbiddenTraitIDs { get; }
    }
}