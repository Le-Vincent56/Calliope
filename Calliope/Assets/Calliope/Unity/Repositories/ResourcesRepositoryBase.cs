using System.Text;
using System.Threading.Tasks;
using Calliope.Infrastructure.Repositories;
using UnityEngine;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a base implementation for a repository that loads and manages data from Unity's Resources system;
    /// This class is specific to managing data of types <typeparamref name="TInterface"/> and <typeparamref name="TScriptableObject"/>
    /// </summary>
    /// <typeparam name="TInterface">
    /// The interface type that the repository will manage. Must be a reference type
    /// </typeparam>
    /// <typeparam name="TScriptableObject">
    /// The type of ScriptableObject that implements <typeparamref name="TInterface"/> and is stored in Unity's Resources folder
    /// </typeparam>
    public abstract class ResourcesRepositoryBase<TInterface, TScriptableObject> : RepositoryBase<TInterface>
        where TScriptableObject : ScriptableObject, TInterface where TInterface : class
    {
        private readonly string _resourcePath;
        
        protected ResourcesRepositoryBase(string resourcePath) 
        {
            _resourcePath = resourcePath;
        }

        /// <summary>
        /// Asynchronously loads and initializes items managed by the repository;
        /// this method is intended to be overridden in derived classes to provide
        /// specific logic for loading items based on the needs of the repository's context
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous operation of loading items;
        /// the task's completion may signify either the success or failure of the load process
        /// </returns>
        protected override Task LoadItemsAsync()
        {
            lock (Lock) 
            {
                if(IsLoaded) return Task.CompletedTask;

                StringBuilder debugBuilder = new StringBuilder();
                debugBuilder.Append("[");
                debugBuilder.Append(GetType().Name);
                debugBuilder.Append("] Loading from Resources: ");
                debugBuilder.Append(_resourcePath);
                Debug.Log(debugBuilder.ToString());
                
                // Load the assets from the Resources folder
                TScriptableObject[] assets = Resources.LoadAll<TScriptableObject>(_resourcePath);

                // Exit case - there are no assets to load
                if (assets == null || assets.Length == 0)
                {
                    // Build the warning message
                    debugBuilder.Clear();
                    debugBuilder.Append("[");
                    debugBuilder.Append(GetType().Name);
                    debugBuilder.Append("] No assets found at Resources/");
                    debugBuilder.Append(_resourcePath);
                    Debug.LogWarning(debugBuilder.ToString());
                    
                    IsLoaded = true;
                    return Task.CompletedTask;
                }

                for (int i = 0; i < assets.Length; i++)
                {
                    TScriptableObject asset = assets[i];
                    
                    // Skip if the asset is null
                    if (!asset) continue;

                    // Get the asset's iD
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
                        debugBuilder.Append("' has no ID");
                        Debug.LogWarning(debugBuilder.ToString());

                        continue;
                    }
                    
                    // Add the asset to the cache
                    AddToCache(asset);
                }

                // Mark the repository as loaded
                IsLoaded = true;
                
                // Build the debug message
                debugBuilder.Clear();
                debugBuilder.Append("[");
                debugBuilder.Append(GetType().Name);
                debugBuilder.Append("] loaded ");
                debugBuilder.Append(Cache.Count);
                debugBuilder.Append(" assets from Resources/");
                debugBuilder.Append(_resourcePath);
                Debug.Log(debugBuilder.ToString());
                
                return Task.CompletedTask;
            }
        }
    }
}