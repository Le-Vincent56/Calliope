namespace Calliope.Core.ValueObjects
{
    /// <summary>
    /// Represents how strongly content relates to a trait;
    /// Positive weight indicates that the character with the trait is more likely to use this;
    /// Negative weight indicates that the character with the trait is less likely to use this
    /// </summary>
    [System.Serializable]
    public struct TraitAffinity
    {
        public string TraitID;
        public float Weight;
        
        public TraitAffinity(string traitID, float weight)
        {
            TraitID = traitID;
            Weight = weight;
        }
    }
}