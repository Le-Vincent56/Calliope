using Calliope.Core.Enums;
using Calliope.Core.Interfaces;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Represents a command used to modify the relationship between two characters;
    /// this command alters the relationship value of a specified type by applying
    /// a given delta and provides support for undoing the change
    /// </summary>
    public class RelationshipCommand : ICommand
    {
        private readonly IRelationshipProvider _relationshipProvider;
        private readonly string _fromCharacterID;
        private readonly string _toCharacterID;
        private readonly RelationshipType _relationshipType;
        private readonly float _delta;
        private bool _executed;
        
        public bool CanUndo { get; set;  }
        public string Description { get; set; }

        public RelationshipCommand(
            IRelationshipProvider relationshipProvider, 
            string fromCharacterID,
            string toCharacterID, 
            RelationshipType relationshipType, 
            float delta,
            bool canUndo = true,
            string description = null
        )
        {
            _relationshipProvider = relationshipProvider;
            _fromCharacterID = fromCharacterID;
            _toCharacterID = toCharacterID;
            _relationshipType = relationshipType;
            _delta = delta;
            _executed = false;
            CanUndo = canUndo;
            Description = description;
        }

        /// <summary>
        /// Executes the relationship modification command;
        /// this method modifies the relationship between two characters by applying
        /// a specified delta to the current relationship value of a specified type;
        /// ensures the command is executed only once by maintaining an internal state
        /// </summary>
        public void Execute()
        {
            // Exit case - the command has already been executed
            if (_executed) return;
            
            // Modify the relationship
            _relationshipProvider.ModifyRelationship(
                _fromCharacterID, 
                _toCharacterID, 
                _relationshipType, 
                _delta
            );
            
            _executed = true;
        }

        /// <summary>
        /// Undoes a previously executed relationship modification command;
        /// reverses the applied delta to restore the original relationship value
        /// and updates the internal state to mark the command as not executed
        /// </summary>
        public void Undo()
        {
            // Exit case - the command has not been executed yet
            if (!_executed) return;
            
            // Reverse the delta
            _relationshipProvider.ModifyRelationship(
                _fromCharacterID, 
                _toCharacterID, 
                _relationshipType, 
                -_delta
            );
            
            _executed = false;
        }
    }
}