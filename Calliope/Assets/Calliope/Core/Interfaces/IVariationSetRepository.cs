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

        /// <summary>
        /// Retrieves a list of dialogue fragments associated with the specified variation set ID.
        /// </summary>
        /// <param name="variationSetID">
        /// The unique identifier of the variation set
        /// </param>
        /// <returns>
        /// A read-only list of <see cref="IDialogueFragment"/> objects associated with the provided variation set ID;
        /// returns an empty list if the variation set with the specified ID is not found
        /// </returns>
        public IReadOnlyList<IDialogueFragment> GetFragmentsBySetID(string variationSetID);
    }
}