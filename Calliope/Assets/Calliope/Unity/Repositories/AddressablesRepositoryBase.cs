using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Calliope.Infrastructure.Repositories;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a base implementation for repositories that manage Unity ScriptableObject resources;
    /// this generic class extends the functionality of <see cref="RepositoryBase{T}"/>
    /// by incorporating support for managing resources stored as ScriptableObjects
    /// </summary>
    /// <typeparam name="TInterface">
    /// The interface type that all managed ScriptableObjects must implement
    /// </typeparam>
    /// <typeparam name="TScriptableObject">
    /// The specific ScriptableObject type used within the repository that implements <typeparamref name="TInterface"/>
    /// </typeparam>
    public abstract class AddressablesRepositoryBase<TInterface, TScriptableObject> : RepositoryBase<TInterface>
        where TScriptableObject : ScriptableObject, TInterface where TInterface : class
    {
        private readonly string _addressableLabel;
        private AsyncOperationHandle<IList<TScriptableObject>> _loadHandle;

        protected AddressablesRepositoryBase(string addressableLabel)
        {
            _addressableLabel = addressableLabel ?? throw new ArgumentNullException(nameof(addressableLabel));
        }

        /// <summary>
        /// Retrieves the unique identifier for the specified item of type <typeparamref name="TInterface"/>;
        /// this method is used to determine the ID of the given item for storage and lookup purposes in the repository
        /// </summary>
        /// <param name="item">The item for which the unique identifier is being retrieved</param>
        /// <returns>A string representing the unique identifier of the item</returns>
        protected abstract override string GetID(TInterface item);

        /// <summary>
        /// Asynchronously loads items into the repository using Addressables and updates the repository's state;
        /// this method ensures proper initialization and populates the repository with the resources
        /// associated with the specified Addressables label
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        protected override async Task LoadItemsAsync()
        {
            // Prevent race conditions by locking for the duration of the operation
            lock (Lock)
            {
                // Exit case - the repository is already loaded
                if (IsLoaded) return;
            }

            // Build the debug message
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("[");
            debugBuilder.Append(GetType().Name);
            debugBuilder.Append("] Loading items from Addressables with label '");
            debugBuilder.Append(_addressableLabel);
            debugBuilder.Append("'");
            Debug.Log(debugBuilder.ToString());

            try
            {
                // Load all assets with this label via Addressables
                _loadHandle = Addressables.LoadAssetsAsync<TScriptableObject>(
                    _addressableLabel,
                    null
                );

                // Await completion
                IList<TScriptableObject> assets = await _loadHandle.Task;

                // Exit case - no assets were found
                if (assets == null || assets.Count == 0)
                {
                    // Build the warning message
                    debugBuilder.Clear();
                    debugBuilder.Append("[");
                    debugBuilder.Append(GetType().Name);
                    debugBuilder.Append("] No assets found with label '");
                    debugBuilder.Append(_addressableLabel);
                    debugBuilder.Append("'");
                    Debug.LogWarning(debugBuilder.ToString());

                    // Lock for thread-safety
                    lock (Lock)
                    {
                        // Mark the repository as loaded
                        IsLoaded = true;
                    }

                    return;
                }

                // Lock for thread-safety
                lock (Lock)
                {
                    // Populate the cache
                    int loadedCount = 0;
                    for (int i = 0; i < assets.Count; i++)
                    {
                        TScriptableObject asset = assets[i];

                        // Skip if the asset is null
                        if (!asset) continue;

                        string id = GetID(asset);

                        // Skip if the asset has no ID
                        if (string.IsNullOrEmpty(id))
                        {
                            // Build the warning message
                            debugBuilder.Clear();
                            debugBuilder.Append("[");
                            debugBuilder.Append(GetType().Name);
                            debugBuilder.Append("] Asset '");
                            debugBuilder.Append(asset.name);
                            debugBuilder.Append("' has no ID, skipping");
                            Debug.LogWarning(debugBuilder.ToString());

                            continue;
                        }

                        // Add the asset to the cache
                        AddToCache(asset);
                        loadedCount++;
                    }

                    // Mark the repository as loaded
                    IsLoaded = true;

                    // Build the debug message
                    debugBuilder.Clear();
                    debugBuilder.Append("[");
                    debugBuilder.Append(GetType().Name);
                    debugBuilder.Append("] Loaded ");
                    debugBuilder.Append(loadedCount);
                    debugBuilder.Append(" assets with label '");
                    debugBuilder.Append(_addressableLabel);
                    debugBuilder.Append("'");
                    Debug.Log(debugBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                // Build the error message
                debugBuilder.Clear();
                debugBuilder.Append("[");
                debugBuilder.Append(GetType().Name);
                debugBuilder.Append("] Failed to load Addressables with label '");
                debugBuilder.Append(_addressableLabel);
                debugBuilder.Append("': ");
                debugBuilder.Append(ex.Message);
                Debug.LogError(debugBuilder.ToString());
                
                // Lock for thread-safety
                lock (Lock)
                {
                    // Mark the repository as loaded
                    IsLoaded = true;
                }
            }
        }

        /// <summary>
        /// Releases the assets loaded through Addressables and clears the repository's internal cache;
        /// this method ensures that resources are properly unloaded and memory is managed effectively
        /// </summary>
        public void ReleaseAssets()
        {
            // Check if the load handle is valid
            if (_loadHandle.IsValid())
            {
                StringBuilder debugBuilder = new StringBuilder();
                
                // Release the Addressables
                Addressables.Release(_loadHandle);
                
                // Build the debug message
                debugBuilder.Append("[");
                debugBuilder.Append(GetType().Name);
                debugBuilder.Append("] Released Addressables with label '");
                debugBuilder.Append(_addressableLabel);
                debugBuilder.Append("'");
                Debug.Log(debugBuilder.ToString());
            }

            // Lock for thread-safety
            lock (Lock)
            {
                // Clear the cache
                ClearCache();
            }
        }
    }
}
