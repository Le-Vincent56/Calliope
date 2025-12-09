using System;
using System.Collections.Generic;
using Calliope.Core.Interfaces;
using Calliope.Core.ValueObjects;

namespace Calliope.Runtime.Services
{
    /// <summary>
    /// Applies context modifiers from dialogue fragments to the scene context;
    /// enables data-driven scene state tracking
    /// </summary>
    public class ContextModifierApplier
    {
        /// <summary>
        /// Applies a collection of context modifiers from a dialogue fragment to the provided scene context
        /// </summary>
        /// <param name="fragment">The dialogue fragment containing the context modifiers to be applied</param>
        /// <param name="context">The scene context where the modifiers will be applied</param>
        public void ApplyModifiers(IDialogueFragment fragment, ISceneContext context)
        {
            // Exit case - no context or context modifiers were provided
            if (fragment?.ContextModifiers == null || context == null) 
                return;

            IReadOnlyList<ContextModifier> modifiers = fragment.ContextModifiers;

            // Apply each modifier
            for (int i = 0; i < modifiers.Count; i++)
            {
                ApplyModifier(modifiers[i], context);
            }
        }

        /// <summary>
        /// Applies a specified context modifier to the given scene context based on the operation type
        /// </summary>
        /// <param name="modifier">The context modifier containing the key, operation, and value to be applied</param>
        /// <param name="context">The scene context where the modifier will be applied</param>
        private void ApplyModifier(ContextModifier modifier, ISceneContext context)
        {
            // Exit case - no key was provided
            if (string.IsNullOrEmpty(modifier.Key)) return;

            switch (modifier.Operation)
            {
                case ContextModifierOperation.Set:
                    context.SetValue(modifier.Key, modifier.Value);
                    break;

                case ContextModifierOperation.Increment:
                    context.Increment(modifier.Key, modifier.Value);
                    break;

                case ContextModifierOperation.Decrement:
                    context.Increment(modifier.Key, -modifier.Value);
                    break;

                case ContextModifierOperation.SetTrue:
                    context.SetValue(modifier.Key, true);
                    break;

                case ContextModifierOperation.SetFalse:
                    context.SetValue(modifier.Key, false);
                    break;

                case ContextModifierOperation.Multiply:
                    float currentMult = context.GetValue<float>(modifier.Key, 1f);
                    context.SetValue(modifier.Key, currentMult * modifier.Value);
                    break;

                case ContextModifierOperation.Max:
                    float currentMax = context.GetValue<float>(modifier.Key, float.MinValue);
                    context.SetValue(modifier.Key, Math.Max(currentMax, modifier.Value));
                    break;

                case ContextModifierOperation.Min:
                    float currentMin = context.GetValue<float>(modifier.Key, float.MaxValue);
                    context.SetValue(modifier.Key, Math.Min(currentMin, modifier.Value));
                    break;
            }
        }
    }
}