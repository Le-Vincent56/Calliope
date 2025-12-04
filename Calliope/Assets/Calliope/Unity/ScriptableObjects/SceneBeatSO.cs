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
    }
}