namespace Calliope.Core.Interfaces
{
    /// <summary>
    /// Defines a faction that characters belong to
    /// </summary>
    public interface IFaction
    {
        /// <summary>
        /// Unique identifier for this faction (e.g., "sealed_circle", "unbound")
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// Human-readable name (e.g., "The Sealed Circle", "The Unbound")
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Description of the faction's beliefs and goals
        /// </summary>
        string Description { get; }
    }
}
