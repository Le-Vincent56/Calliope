using System.Collections.Generic;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A repository for accessing character definitions
    /// </summary>
    public interface ICharacterRepository : IRepository<ICharacter>
    {
        /// <summary>
        /// Retrieves a list of characters that possess the specified trait;
        /// useful for casting: "Find all aggressive characters";
        /// Example: GetByTrait("aggressive") returns Aldric if he has that trait
        /// </summary>
        /// <param name="traitID">The unique identifier of the trait to filter characters by</param>
        /// <returns>A read-only list of characters that have the specified trait</returns>
        IReadOnlyList<ICharacter> GetByTrait(string traitID);
    }
}