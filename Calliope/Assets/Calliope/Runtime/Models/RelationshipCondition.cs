using System.Collections.Generic;
using System.Text;
using Calliope.Core.Enums;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Models
{
    /// <summary>
    /// Branch condition based on the relationship value between two roles;
    /// Example: "Mediator's opinion of Instigator >= 60"
    /// </summary>
    public class RelationshipCondition : IBranchCondition
    {
        public string FromRoleID { get; set; }
        public string ToRoleID { get; set; }
        public RelationshipType Type { get; set; }
        public float Threshold { get; set; }
        public bool GreaterThan { get; set; }

        /// <summary>
        /// Evaluates whether the relationship condition between two characters is satisfied
        /// based on the specified relationship type, threshold, and comparison mode
        /// </summary>
        /// <param name="cast">
        /// A dictionary of character roles mapped to their respective character instances
        /// </param>
        /// <param name="relationships">
        /// An instance of an IRelationshipProvider to query relationship values between characters
        /// </param>
        /// <param name="sceneContext">Optional scene-specific context storing key-value pairs relevant to the evaluation</param>
        /// <returns>
        /// True if the relationship condition is satisfied, otherwise false
        /// </returns>
        public bool Evaluate(IReadOnlyDictionary<string, ICharacter> cast, IRelationshipProvider relationships, ISceneContext sceneContext = null)
        {
            // Exit cases - the characters are not found from the roles
            if (!cast.TryGetValue(FromRoleID, out ICharacter fromChar)) return false;
            if(!cast.TryGetValue(ToRoleID, out ICharacter toChar)) return false;
            
            // Get the relationship value between the two characters
            float value = relationships.GetRelationship(fromChar.ID, toChar.ID, Type);
            
            // Compare the value to the threshold
            return GreaterThan 
                ? value > Threshold 
                : value < Threshold;
        }

        public string GetDescription()
        {
            StringBuilder descriptionBuilder = new StringBuilder();
            string op = GreaterThan ? ">=" : "<=";
            
            // Build the description string
            descriptionBuilder.Append(FromRoleID);
            descriptionBuilder.Append("'s ");
            descriptionBuilder.Append(Type);
            descriptionBuilder.Append(" toward ");
            descriptionBuilder.Append(ToRoleID);
            descriptionBuilder.Append(" is ");
            descriptionBuilder.Append(op);
            descriptionBuilder.Append(Threshold);
            
            return descriptionBuilder.ToString();
        }
    }
}