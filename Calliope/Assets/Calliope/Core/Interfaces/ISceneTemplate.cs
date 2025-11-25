using System.Collections.Generic;
using Calliope.Core.Enums;

namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// A complete template for a narrative scene; defines roles, beats, and flow
    /// Example: "Campfire Disagreement" scene
    /// </summary>
    public interface ISceneTemplate
    {
        /// <summary>
        /// Unique identifier ("campfire_disagreement")
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Display name shown ("Campfire Disagreement")
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Description of what happens in this scene
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// The type of trigger that starts this scene
        /// </summary>
        SceneTriggerType TriggerType { get; }
        
        /// <summary>
        /// All the possible roles that need to be filled for this scene;
        /// Example: ["Instigator", "Objector", "Mediator"]
        /// </summary>
        IReadOnlyList<ISceneRole> Roles { get; }
        
        /// <summary>
        /// ID of the first beat in this scene
        /// </summary>
        string StartingBeatID { get; }
        
        /// <summary>
        /// All the beats in this scene, indexed by beat ID;
        /// Example: { "beat_1" => ISceneBeat, "beat_2" => ISceneBeat }
        /// </summary>
        IReadOnlyDictionary<string, ISceneBeat> Beats { get; }
    }
}