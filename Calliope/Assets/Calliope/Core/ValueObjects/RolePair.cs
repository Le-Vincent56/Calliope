using System;

namespace Calliope.Core.ValueObjects
{
    /// <summary>
    /// A pair of role IDs representing a directional relationship;
    /// used by conditions that need to evaluate relationships between
    /// multiple role pairs
    /// </summary>
    [Serializable]
    public class RolePair
    {
        public string FromRoleID;
        public string ToRoleID;
        
        public RolePair(string fromRoleID, string toRoleID)
        {
            FromRoleID = fromRoleID;
            ToRoleID = toRoleID;
        }
    }
}
