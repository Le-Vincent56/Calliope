using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject implementation of ISceneTemplate;
    /// allows scene templates to be created as Unity assets and edited in the Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "New Scene Template", menuName = "Calliope/Scene Template", order = 6)]
    public class SceneTemplateSO : ScriptableObject, ISceneTemplate
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this scene (e.g., 'campfire_disagreement'")]
        [SerializeField] private string id;
        
        [Tooltip("Display name shown in the editor (e.g., 'Campfire Disagreement')")]
        [SerializeField] private string displayName;
        
        [Header("Trigger")]
        [Tooltip("How the scene is triggered")]
        [SerializeField] private SceneTriggerType triggerType = SceneTriggerType.Manual;
        
        [Header("Structure")]
        [Tooltip("Roles that need to be cast for this scene")]
        [SerializeField] private SceneRoleSO[] roles;
        
        [Tooltip("All beats (narrative moments) in this scene")]
        [SerializeField] private SceneBeatSO[] beats;
        
        [Tooltip("ID of the beat to start the scene at")]
        [SerializeField] private string startingBeatID;
        
        [Header("Metadata")]
        [Tooltip("Description of what happens in this scene")]
        [SerializeField] [TextArea(3, 6)] private string description;
        
        [Tooltip("Tags for filtering and organization")]
        [SerializeField] private string[] tags;

        private Dictionary<string, ISceneBeat> _beatDictionary;
        
        public string ID => id;
        public string DisplayName => displayName;
        public SceneTriggerType TriggerType => triggerType;
        public IReadOnlyList<ISceneRole> Roles => roles;
        public IReadOnlyDictionary<string, ISceneBeat> Beats => GetBeatDictionary();
        public string StartingBeatID => startingBeatID;
        public string Description => description;
        public IReadOnlyList<string> Tags => tags;

        private IReadOnlyDictionary<string, ISceneBeat> GetBeatDictionary()
        {
            // Exit case - beat dictionary is already cached
            if (_beatDictionary != null) return _beatDictionary;
            
            _beatDictionary = new Dictionary<string, ISceneBeat>();

            // Exit case - no beats were provided
            if (beats == null) return _beatDictionary;
                
            for (int i = 0; i < beats.Length; i++)
            {
                SceneBeatSO beat = beats[i];
                if (beat && !string.IsNullOrEmpty(beat.BeatID))
                {
                    _beatDictionary.Add(beat.BeatID, beat);
                }
            }

            return _beatDictionary;
        }
    }
}