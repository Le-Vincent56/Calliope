using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Infrastructure.Events
{
    /// <summary>
    /// Fired when characters have been cast into scene roles;
    /// useful for debugging: "Who got cast as what?"
    /// </summary>
    public class SceneCastEvent : CalliopeEventBase
    {
        /// <summary>
        /// The ID of the scene that was cast
        /// </summary>
        public string SceneID { get; }
        
        /// <summary>
        /// The mapping of role ID to characters;
        /// Example: { "instigator" => Aldric, "objector" => Sera, "mediator" => Sera }"
        /// </summary>
        public IReadOnlyDictionary<string, ICharacter> Cast { get; }
        
        public SceneCastEvent(string sceneID, IReadOnlyDictionary<string, ICharacter> cast)
        {
            SceneID = sceneID;
            Cast = cast;
        }
    }
}