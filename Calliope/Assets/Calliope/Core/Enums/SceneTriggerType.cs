namespace Calliope.Core.Enums
{
    /// <summary>
    /// Types of triggers that can be used to trigger a dialogue scene
    /// </summary>
    public enum SceneTriggerType
    {
        Manual,         // Triggered by code/script
        Automatic,      // Triggered when conditions are met
        PlayerChoice    // Triggered by player selecting from menu
    }
}