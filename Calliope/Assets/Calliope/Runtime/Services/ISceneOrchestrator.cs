using System.Collections.Generic;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Interface for scene orchestration
    /// </summary>
    public interface ISceneOrchestrator
    {
        bool StartScene(ISceneTemplate scene, IReadOnlyDictionary<string, ICharacter> cast);
        ISceneBeat GetCurrentBeat();
        ICharacter GetCharacterForRole(string roleID);
        bool AdvanceToNextBeat();
        bool IsSceneActive();
        void EndScene();
    }
}