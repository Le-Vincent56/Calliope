using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using UnityEngine;

namespace Calliope.Unity.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Scene Beat", menuName = "Calliope/Scene Beat", order = 8)]
    public class SceneBeatSO : ScriptableObject, ISceneBeat
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this scene beat (e.g., 'beat_1', 'beat_4a')")]
        [SerializeField] private string beatID;
        
        [Header("Participants")]
        [Tooltip("Role ID of the character speaking in this beat")]
        [SerializeField] private string speakerRoleID;
        
        [Tooltip("Role ID of the character being spoken to (can be empty for narration/monologue")]
        [SerializeField] private string targetRoleID;
        
        [Header("Content")]
        [Tooltip("The variation set containing all possible dialogue fragments for this beat")]
        [SerializeField] private VariationSetSO variationSet;
        
        [Header("Flow")]
        [Tooltip("If true, this beat ends the scene (no further beats")]
        [SerializeField] private bool isEndBeat;
        
        [Tooltip("Branches to next beats; first matching branch is taken; empty means the scene ends")]
        [SerializeField] private BeatBranchSO[] branches;
        
        [Tooltip("Beat to transition to if no branches match (can be empty if this is an end beat")]
        [SerializeField] private string defaultNextBeatID;
        
        public string BeatID => beatID;
        public string SpeakerRoleID => speakerRoleID;
        public string TargetRoleID => targetRoleID;
        public string VariationSetID => variationSet ? variationSet.ID : null;
        public IReadOnlyList<IBeatBranch> Branches => branches;
        public string DefaultNextBeatID => defaultNextBeatID;
        public bool IsEndBeat => isEndBeat;

        private void OnValidate()
        {
            // Auto-generate ID from the asset name if empty
            if (string.IsNullOrEmpty(beatID))
                beatID = name;
            
            StringBuilder warningBuilder = new StringBuilder();
            
            // Validate the speaker role
            if (string.IsNullOrEmpty(speakerRoleID))
            {
                warningBuilder.Append("[SceneBeatSO] '");
                warningBuilder.Append(name);
                warningBuilder.Append("' has no speaker role ID specified");
                
                Debug.LogWarning(warningBuilder.ToString(), this);
            }
            
            // Validate the variation set
            if (!variationSet)
            {
                warningBuilder.Clear();
                warningBuilder.Append("[SceneBeatSO] '");
                warningBuilder.Append(name);
                warningBuilder.Append("' has no variation set assigned");
                
                Debug.LogWarning(warningBuilder.ToString(), this);           
            }
            
            // Validate flow logic
            bool hasBranches = branches != null && branches.Length > 0;
            bool hasDefaultNext = !string.IsNullOrEmpty(defaultNextBeatID);

            if (isEndBeat)
            {
                // End beats shouldn't have branches or a default next beat
                if (hasBranches)
                {
                    warningBuilder.Clear();
                    warningBuilder.Append("[SceneBeatSO] End beat '");
                    warningBuilder.Append(name);
                    warningBuilder.Append("' is marked as an end beat but has branches; branches will be ignored");
                    
                    Debug.LogWarning(warningBuilder.ToString(), this);
                }

                if (hasDefaultNext)
                {
                    warningBuilder.Clear();
                    warningBuilder.Append("[SceneBeatSO] End beat '");
                    warningBuilder.Append(name);
                    warningBuilder.Append("' is marked as an end beat but has a default next beat; it will be ignored");

                    Debug.LogWarning(warningBuilder.ToString(), this);
                }
            }
            else
            {
                // Non-end beats should have at least one branch or a default next beat
                if (!hasBranches && !hasDefaultNext)
                {
                    warningBuilder.Clear();
                    warningBuilder.Append("[SceneBeatSO] '");
                    warningBuilder.Append(name);
                    warningBuilder.Append("' is not an end beat but has no branches or default next beat; the scene will end here");
                    
                    Debug.LogWarning(warningBuilder.ToString(), this);
                }
            }

            // Exit case - there are no branches
            if (!hasBranches) return;
            
            int nullCount = 0;
            for (int i = 0; i < branches.Length; i++)
            {
                // Skip over valid branches
                if (branches[i]) continue;

                nullCount++;
            }

            // Exit case - no null branches
            if (nullCount <= 0) return;
                
            warningBuilder.Clear();
            warningBuilder.Append("[SceneBeatSO] '");
            warningBuilder.Append(name);
            warningBuilder.Append("' has ");
            warningBuilder.Append(nullCount);
            warningBuilder.Append(" null branches; assign BeatBranchSO assets");
                    
            Debug.LogWarning(warningBuilder.ToString(), this);
        }
        
        
    }
}