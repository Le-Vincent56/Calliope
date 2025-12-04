using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject representation of a faction
    /// </summary>
    [CreateAssetMenu(fileName = "New Faction", menuName = "Calliope/Faction", order = 2)]
    public class FactionSO : ScriptableObject, IFaction
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this faction (e.g., 'sealed_circle', 'unbound')")]
        [SerializeField] private string id;
        
        [Tooltip("Display name shown in UI (e.g., 'Sealed Circle', 'Unbound')")]
        [SerializeField] private string displayName;
        
        [Header("Description")]
        [Tooltip("What this faction believes and fights for")]
        [TextArea(3, 6)] [SerializeField] private string description;
        
        public string ID => id;
        public string DisplayName => displayName;
        public string Description => description;

        private void OnValidate()
        {
            StringBuilder validationBuilder = new StringBuilder();
            
            if (string.IsNullOrEmpty(id))
            {
                validationBuilder.Clear();
                validationBuilder.Append("[FactionSO] '");
                validationBuilder.Append(name);
                validationBuilder.Append("' has no ID set");
                
                Debug.LogWarning(validationBuilder.ToString(), this);
            }

            if (string.IsNullOrEmpty(displayName))
            {
                validationBuilder.Clear();
                validationBuilder.Append("[FactionSO] '");
                validationBuilder.Append(name);
                validationBuilder.Append("' has no display name set");
                
                Debug.LogWarning(validationBuilder.ToString(), this);
            }
        }
    }
}
