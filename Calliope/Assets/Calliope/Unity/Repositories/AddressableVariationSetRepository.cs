using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a repository implementation for managing VariationSet data;
    /// Expects variation sets to be tagged with the "VariationSets" label in Addressables
    /// </summary>
    public class AddressableVariationSetRepository : AddressablesRepositoryBase<IVariationSet, VariationSetSO>,
        IVariationSetRepository
    {
        public AddressableVariationSetRepository() : base("Variation Set") { }

        /// <summary>
        /// Retrieves the unique identifier for a given variation set
        /// </summary>
        /// <param name="item">
        /// The variation set instance for which the identifier will be retrieved
        /// </param>
        /// <returns>
        /// A string representing the unique identifier of the specified variation set
        /// </returns>
        protected override string GetID(IVariationSet item) => item.ID;

        /// <summary>
        /// Retrieves all dialogue fragments from all variation sets available in the repository
        /// </summary>
        /// <returns>
        /// A read-only list of <c>IDialogueFragment</c> objects comprising all fragments contained in the available variation sets;
        /// Returns an empty list if no fragments are present or no variation sets contain valid fragments
        /// </returns>
        public IReadOnlyList<IDialogueFragment> GetAllFragments()
        {
            IReadOnlyList<IVariationSet> allSets = GetAll();
            List<IDialogueFragment> allFragments = new List<IDialogueFragment>();

            // Add all variations from all variation sets
            for (int i = 0; i < allSets.Count; i++)
            {
                IReadOnlyList<IDialogueFragment> variations = allSets[i].Variations;
                
                // Skip if the variation set does not have any variations
                if (variations == null) continue;

                for (int j = 0; j < variations.Count; j++)
                {
                    // Skip if the variation is null
                    if (variations[j] == null) continue;
                    
                    allFragments.Add(variations[j]);
                }
            }

            return allFragments;
        }

        /// <summary>
        /// Retrieves a list of dialogue fragments associated with the specified variation set ID
        /// </summary>
        /// <param name="variationSetID">The unique identifier of the variation set</param>
        /// <returns>
        /// A read-only list of <c>IDialogueFragment</c> objects associated with the provided variation set ID;
        /// Returns an empty list if the variation set with the specified ID is not found
        /// </returns>
        public IReadOnlyList<IDialogueFragment> GetFragmentsBySetID(string variationSetID)
        {
            IVariationSet set = GetByID(variationSetID);
            
            return set == null 
                ? new List<IDialogueFragment>() 
                : set.Variations;
        }
    }
}