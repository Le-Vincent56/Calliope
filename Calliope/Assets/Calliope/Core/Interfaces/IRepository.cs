using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Generic repository for accessing content; abstracts the data source (ScriptableObjects,
    /// JSON, database, etc.)
    /// </summary>
    /// <typeparam name="T">Type of content (ITrait, ICharacter, etc.)</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves an item of type <typeparamref name="T"/> by its identifier
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="id">The unique identifier of the item.</param>
        /// <returns>
        /// The item of type <typeparamref name="T"/> that matches the
        /// specified identifier, or null if not found
        /// </returns>
        T GetByID(string id);

        /// <summary>
        /// Asynchronously retrieves an item of type <typeparamref name="T"/> by its identifier
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve</typeparam>
        /// <param name="id">The unique identifier of the item</param>
        /// <returns>
        /// A task that represents the asynchronous operation; the task result
        /// contains the item of type <typeparamref name="T"/> that matches
        /// the specified identifier, or null if not found
        /// </returns>
        Task<T> GetByIDAsync(string id);

        /// <summary>
        /// Retrieves all items of type <typeparamref name="T"/> from the repository
        /// </summary>
        /// <typeparam name="T">The type of the items to retrieve</typeparam>
        /// <returns>
        /// A read-only list containing all items of type <typeparamref name="T"/>
        /// in the repository.
        /// </returns>
        IReadOnlyList<T> GetAll();

        /// <summary>
        /// Asynchronously retrieves all items of type <typeparamref name="T"/> from the repository
        /// </summary>
        /// <typeparam name="T">The type of the items to retrieve</typeparam>
        /// <returns>
        /// A task that represents the asynchronous operation; the task result contains
        /// a read-only list of all items of type <typeparamref name="T"/> in the repository
        /// </returns>
        Task<IReadOnlyList<T>> GetAllAsync();

        /// <summary>
        /// Checks whether an item with the specified identifier exists in the repository
        /// </summary>
        /// <param name="id">The unique identifier of the item to check for existence</param>
        /// <returns>
        /// True if an item with the specified identifier exists in the repository; otherwise, false
        /// </returns>
        bool Exists(string id);
    }
}