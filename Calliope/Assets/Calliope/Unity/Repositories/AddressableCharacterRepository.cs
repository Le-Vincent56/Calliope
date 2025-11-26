using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a repository implementation for managing Character data;
    /// Expects characters to be tagged with the "Characters" label in Addressables
    /// </summary>
    public class AddressableCharacterRepository : AddressablesRepositoryBase<ICharacter, CharacterSO>, ICharacterRepository
    {
        public AddressableCharacterRepository() : base("Character") { }

        /// <summary>
        /// Retrieves the unique identifier of the specified character
        /// </summary>
        /// <param name="item">The character whose identifier is being retrieved</param>
        /// <returns>A string representing the unique identifier of the character</returns>
        protected override string GetID(ICharacter item) => item.ID;

        /// <summary>
        /// Retrieves a list of characters that possess a specific trait
        /// </summary>
        /// <param name="traitID">The unique identifier of the trait to filter characters by</param>
        /// <returns>A read-only list of characters that have the specified trait</returns>
        public IReadOnlyList<ICharacter> GetByTrait(string traitID)
        {
            IReadOnlyList<ICharacter> allCharacters = GetAll();
            List<ICharacter> result = new List<ICharacter>();
            
            for(int i = 0; i < allCharacters.Count; i++)
            {
                // Skip if the character does not possess the trait
                if (!allCharacters[i].HasTrait(traitID)) continue;
                    
                result.Add(allCharacters[i]);
            }
            
            return result;
        }

        /// <summary>
        /// Retrieves a list of characters that possess all the specified traits
        /// </summary>
        /// <param name="traitIDs">A collection of trait IDs that the characters must possess</param>
        /// <returns>A read-only list of characters that match the specified traits</returns>
        public IReadOnlyList<ICharacter> GetByTraits(IEnumerable<string> traitIDs)
        {
            IReadOnlyList<string> traitIDsList = traitIDs as IReadOnlyList<string> ?? new List<string>(traitIDs);
            IReadOnlyList<ICharacter> allCharacters = GetAll();
            List<ICharacter> result = new List<ICharacter>();
            
            for(int i = 0; i < allCharacters.Count; i++)
            {
                // Skip if the character does not possess all of the traits
                if (!allCharacters[i].HasAllTraits(traitIDsList)) continue;
                    
                result.Add(allCharacters[i]);
            }
            
            return result;
        }
    }
}