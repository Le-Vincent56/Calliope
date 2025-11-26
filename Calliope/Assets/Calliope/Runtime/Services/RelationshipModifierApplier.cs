using System;
using System.Collections.Generic;
using System.Text;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;
using Calliope.Infrastructure.Logging;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Applies relationship modifiers from dialogue fragments;
    /// called after a line is spoken to update character relationships
    /// </summary>
    public class RelationshipModifierApplier
    {
        private readonly IRelationshipProvider _relationshipProvider;
        private readonly ILogger _logger;

        public RelationshipModifierApplier(IRelationshipProvider relationshipProvider, ILogger logger)
        {
            _relationshipProvider = relationshipProvider ?? throw new ArgumentNullException(nameof(relationshipProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Applies a set of relationship modifiers, derived from the provided dialogue fragment,
        /// to adjust the relationship values among the specified speaker and target characters
        /// </summary>
        /// <param name="fragment">The dialogue fragment containing the relationship modifiers to apply; if null, no adjustments are performed</param>
        /// <param name="speaker">The character initiating the relationship adjustment; if null, no adjustments are made</param>
        /// <param name="target">The character targeted for the relationship adjustment; can be null for self-directed adjustments or no target</param>
        public void ApplyModifiers(IDialogueFragment fragment, ICharacter speaker, ICharacter target)
        {
            // Exit case - no fragment or speaker were provided
            if (fragment == null || speaker == null) return;
            
            IReadOnlyList<RelationshipModifier> modifiers = fragment.RelationshipModifiers;
            
            // Exit case - no modifiers were found
            if (modifiers == null || modifiers.Count == 0) return;
            
            // Apply each modifier
            for (int i = 0; i < modifiers.Count; i++)
            {
                RelationshipModifier modifier = modifiers[i];
                ApplyModifier(modifier, speaker, target);
            }
        }

        /// <summary>
        /// Applies a single relationship modifier to adjust the relationship value between the specified speaker and target characters
        /// </summary>
        /// <param name="modifier">The relationship modifier that specifies the type, threshold, and multiplier for the adjustment</param>
        /// <param name="speaker">The character initiating the relationship adjustment</param>
        /// <param name="target">The character targeted for the relationship adjustment; if null, no adjustment is made</param>
        private void ApplyModifier(RelationshipModifier modifier, ICharacter speaker, ICharacter target)
        {
            // Exit case - no target character was provided
            if (target == null)
            {
                _logger.LogWarning("[RelationshipModifierApplier] Cannot apply modifier: no target character");
                return;
            }
            
            // Get the current relationship value
            float currentValue = _relationshipProvider.GetRelationship(speaker.ID, target.ID, modifier.Type);

            StringBuilder debugBuilder = new StringBuilder();
            
            // Check if the threshold is met
            if (currentValue < modifier.Threshold)
            {
                // Build debug message
                debugBuilder.Append("[RelationshipModifierApplier] Modifier skipped: ");
                debugBuilder.Append(speaker.ID);
                debugBuilder.Append(" to ");
                debugBuilder.Append(target.ID);
                debugBuilder.Append(" (");
                debugBuilder.Append(modifier.Type);
                debugBuilder.Append("): ");
                debugBuilder.Append(currentValue.ToString("F1"));
                debugBuilder.Append(" < ");
                debugBuilder.Append(modifier.Threshold.ToString("F1"));
                
                _logger.LogDebug(debugBuilder.ToString());
                return;
            }
            
            // Apply the multiplier as a delta
            // Multiplier > 1.0 = increase, < 1.0 = decrease
            // Example: currentValue = 60, multiplier = 1.2: delta = +12
            float delta = currentValue * (modifier.Multiplier - 1f);

            // Exit case - no meaningful change was made
            if (Math.Abs(delta) < 0.01f) return;
            
            // Apply the change
            _relationshipProvider.ModifyRelationship(
                speaker.ID, 
                target.ID, 
                modifier.Type, 
                delta
            );

            // Build the debug message
            debugBuilder.Append("[RelationshipModifierApplier] Applied modifier: ");
            debugBuilder.Append(speaker.ID);
            debugBuilder.Append(" to ");
            debugBuilder.Append(target.ID);
            debugBuilder.Append(" (");
            debugBuilder.Append(modifier.Type);
            debugBuilder.Append("): ");
            debugBuilder.Append(currentValue.ToString("F1"));
            debugBuilder.Append(" + ");
            debugBuilder.Append(delta.ToString("F1"));
            debugBuilder.Append(" = ");
            debugBuilder.Append((currentValue + delta).ToString("F1"));
            
            _logger.LogDebug(debugBuilder.ToString());
        }

        /// <summary>
        /// Applies relationship modifiers from the provided dialogue fragment as commands
        /// and immediately executes them if conditions are met
        /// </summary>
        /// <param name="fragment">The dialogue fragment that contains relationship modifiers to be applied</param>
        /// <param name="speaker">The character initiating the action or dialogue</param>
        /// <param name="target">The character targeted by the action or dialogue</param>
        /// <returns>A list of commands that were executed based on the relationship modifiers</returns>
        public List<ICommand> ApplyModifiersAsCommands(
            IDialogueFragment fragment,
            ICharacter speaker,
            ICharacter target
        )
        {
            List<ICommand> commands = new List<ICommand>();

            // Exit case - no fragment, speaker, or target were provided
            if (fragment == null || speaker == null || target == null)
                return commands;
            
            // Get the modifiers for the fragment
            IReadOnlyList<RelationshipModifier> modifiers = fragment.RelationshipModifiers;
            
            // Exit case - no modifiers were found
            if (modifiers == null || modifiers.Count == 0) return commands;
            
            // Create and execute commands for each modifier
            for (int i = 0; i < modifiers.Count; i++)
            {
                RelationshipModifier modifier = modifiers[i];
                
                // Get the current value
                float currentValue = _relationshipProvider.GetRelationship(
                    speaker.ID,
                    target.ID,
                    modifier.Type
                );
                
                // Skip if the value is under the threshold
                if (currentValue < modifier.Threshold) continue;
                
                // Calculate delta
                float delta = currentValue * (modifier.Multiplier - 1f);

                // Skip if the delta is not significant
                if (Math.Abs(delta) < 0.01f) continue;
                
                // Create the command
                ICommand command = new RelationshipCommand(
                    _relationshipProvider,
                    speaker.ID,
                    target.ID,
                    modifier.Type,
                    delta
                );
                
                // Execute immediately
                command.Execute();
                commands.Add(command);
            }

            return commands;
        }
    }
}