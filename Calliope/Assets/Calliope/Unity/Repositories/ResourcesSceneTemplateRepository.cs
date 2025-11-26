using Calliope.Core.Interfaces;
using Calliope.Unity.ScriptableObjects;

namespace Calliope.Unity.Repositories
{
    /// <summary>
    /// Provides a repository implementation for managing SceneTemplate data;
    /// Expects scene templates to be stored in the "Calliope/Scenes" folder in the Resources folder
    /// </summary>
    public class ResourcesSceneTemplateRepository : ResourcesRepositoryBase<ISceneTemplate, SceneTemplateSO>,
        ISceneTemplateRepository
    {
        public ResourcesSceneTemplateRepository() : base("Calliope/Scenes") { }
        
        /// <summary>
        /// Retrieves the unique identifier for a specified scene template
        /// </summary>
        /// <param name="item">
        /// The scene template instance from which the unique identifier will be retrieved
        /// </param>
        /// <returns>
        /// A string representing the unique identifier of the given scene template
        /// </returns>
        protected override string GetID(ISceneTemplate item) => item.ID;
    }
}