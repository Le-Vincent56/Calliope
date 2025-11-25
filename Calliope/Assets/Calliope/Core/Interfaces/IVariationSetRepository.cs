using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Repository for accessing variation sets (collections of dialogue fragments)
    /// </summary>
    public interface IVariationSetRepository : IRepository<IVariationSet>
    {
        /// <summary>
        /// Retrieves all dialogue fragments from all variation sets in the repository;
        /// useful for validation: "Check all fragments for errors"
        /// </summary>
        /// <returns>
        /// A read-only list of dialogue fragments, each represented as an instance of <see cref="IDialogueFragment"/>
        /// </returns>
        IReadOnlyList<IDialogueFragment> GetAllFragments();
    }
}