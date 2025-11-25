using System.Collections.Generic;
using System.Threading.Tasks;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Repositories
{
    /// <summary>
    /// Provides a base implementation for repository functionality with caching and
    /// asynchronous support for managing data of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the data managed by the repository. Must be a reference type</typeparam>
    public abstract class RepositoryBase<T> : IRepository<T> where T : class
    {
        protected readonly Dictionary<string, T> Cache = new Dictionary<string, T>();
        protected bool IsLoaded = false;
        protected readonly object Lock = new object();

        /// <summary>
        /// Retrieves the unique identifier of the specified item
        /// </summary>
        /// <param name="item">The item for which the unique identifier is to be retrieved</param>
        /// <returns>The unique identifier of the provided item as a string</returns>
        protected abstract string GetID(T item);

        /// <summary>
        /// Asynchronously loads and caches a collection of items of type <typeparamref name="T"/>
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        protected abstract Task LoadItemsAsync();

        /// <summary>
        /// Retrieves an item from the repository by its unique identifier
        /// </summary>
        /// <param name="id">The unique identifier of the item to retrieve</param>
        /// <returns>The item associated with the specified unique identifier, or null if not found</returns>
        public T GetByID(string id)
        {
            // Ensure the repository is loaded
            EnsureLoaded();

            // Lock for thread-safety
            lock (Lock)
            {
                return Cache.GetValueOrDefault(id);
            }
        }

        /// <summary>
        /// Asynchronously retrieves an item by its unique identifier from the repository
        /// </summary>
        /// <param name="id">The unique identifier of the item to retrieve</param>
        /// <returns>A task that represents the asynchronous operation, containing the item if found; otherwise, null</returns>
        public async Task<T> GetByIDAsync(string id)
        {
            // Ensure the repository is loaded
            await EnsureLoadedAsync();

            // Prevent race conditions by locking for the duration of the operation
            lock (Lock)
            {
                return Cache.GetValueOrDefault(id);
            }
        }

        /// <summary>
        /// Retrieves all items stored in the repository
        /// </summary>
        /// <returns>A read-only list containing all items in the repository</returns>
        public IReadOnlyList<T> GetAll()
        {
            // Ensure the repository is loaded
            EnsureLoaded();
            
            // Lock for thread-safety
            lock (Lock)
            {
                List<T> items = new List<T>(Cache.Count);
                
                // Add all the items to the list
                foreach (KeyValuePair<string, T> kvp in Cache)
                {
                    items.Add(kvp.Value);
                }

                return items;
            }
        }

        /// <summary>
        /// Retrieves all items from the repository asynchronously
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing a read-only list of all items in the repository</returns>
        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            // Ensure the repository is loaded
            await EnsureLoadedAsync();

            // Prevent race conditions by locking for the duration of the operation
            lock (Lock)
            {
                List<T> items = new List<T>(Cache.Count);
                
                // Add all the items to the list
                foreach (KeyValuePair<string, T> kvp in Cache)
                {
                    items.Add(kvp.Value);
                }

                return items;
            }
        }

        /// <summary>
        /// Checks whether an item with the specified identifier exists in the repository's cache
        /// </summary>
        /// <param name="id">The unique identifier of the item to check for existence</param>
        /// <returns><c>true</c> if the item with the specified identifier exists; otherwise, <c>false</c></returns>
        public bool Exists(string id)
        {
            // Ensure the repository is loaded
            EnsureLoaded();

            // Lock for thread-safety
            lock (Lock)
            {
                return Cache.ContainsKey(id);
            }
        }

        /// <summary>
        /// Ensures that the repository is loaded by invoking the loading logic, if it has not already been executed
        /// </summary>
        /// <remarks>
        /// This method blocks the current thread until the repository's items are fully loaded into memory
        /// </remarks>
        protected void EnsureLoaded()
        {
            // Exit case - the repository is already loaded
            if (IsLoaded) return;
            
            // Synchronous fallback - blocks until laoded
            LoadItemsAsync().Wait();
        }

        /// <summary>
        /// Ensures that the repository is asynchronously loaded by invoking the underlying load logic
        /// if it has not already been performed
        /// </summary>
        /// <returns>A task that represents the asynchronous load operation</returns>
        protected async Task EnsureLoadedAsync()
        {
            // Exit case - the repository is already loaded
            if (IsLoaded) return;

            await LoadItemsAsync();
        }

        /// <summary>
        /// Adds the specified item to the cache, updating the entry if it already exists
        /// </summary>
        /// <param name="item">The item to be added to the cache</param>
        protected void AddToCache(T item)
        {
            // Exit case - the item is null
            string id = GetID(item);

            // Lock for thread-safety
            lock (Lock)
            {
                Cache[id] = item;
            }
        }

        /// <summary>
        /// Clears all cached items and resets the loaded state
        /// </summary>
        protected void ClearCache()
        {
            // Lock for thread-safety
            lock (Lock)
            {
                Cache.Clear();
                IsLoaded = false;
            }
        }
    }
}