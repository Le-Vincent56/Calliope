using System.Collections.Generic;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Represents a scene template, which is a collection of beats and roles that define a narrative scene
    /// </summary>
    public class SceneTemplate : ISceneTemplate
    {
        public string ID { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set;  }
        public SceneTriggerType TriggerType { get; set; }
        public IReadOnlyList<ISceneRole> Roles { get; set; }
        public string StartingBeatID { get; set; }
        public IReadOnlyDictionary<string, ISceneBeat> Beats { get; set; }
        
        public SceneTemplate()
        {
            Roles = new List<ISceneRole>();
            Beats = new Dictionary<string, ISceneBeat>();
        }
    }
}