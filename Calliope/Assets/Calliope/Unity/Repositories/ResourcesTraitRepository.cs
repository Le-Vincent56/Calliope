using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a repository implementation for managing Trait data;
    /// Expects traits to be in the "Calliope/Traits" folder in "Assets/Resources"
    /// </summary>
    public class ResourcesTraitRepository : ResourcesRepositoryBase<ITrait, TraitSO>, ITraitRepository
    {
        public ResourcesTraitRepository() : base("Calliope/Traits") { }

        /// <summary>
        /// Retrieves the unique identifier for the specified ITrait item
        /// </summary>
        /// <param name="item">
        /// An instance of ITrait from which the ID will be obtained
        /// </param>
        /// <returns>
        /// A string representing the unique identifier of the specified ITrait item
        /// </returns>
        protected override string GetID(ITrait item) => item.ID;
    }
}