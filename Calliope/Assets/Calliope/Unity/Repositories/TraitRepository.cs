using System.ComponentModel;
using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a repository implementation for managing Trait data;
    /// Expects traits to be tagged with the "Traits" label in Addressables
    /// </summary>
    public class TraitRepository : AddressablesRepositoryBase<ITrait, TraitSO>, ITraitRepository
    {
        public TraitRepository() : base("Trait") { }

        /// <summary>
        /// Retrieves the unique identifier for the specified ITrait item
        /// </summary>
        /// <param name="item">The ITrait item from which to extract the identifier</param>
        /// <returns>The unique identifier of the specified ITrait item as a string</returns>
        protected override string GetID(ITrait item) => item.ID;
    }
}