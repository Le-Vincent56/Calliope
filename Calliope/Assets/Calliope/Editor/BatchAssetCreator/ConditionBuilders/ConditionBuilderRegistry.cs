using System;
using System.Collections.Generic;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    /// <summary>
    /// A registry for managing and accessing implementations of <see cref="IConditionRowBuilder"/>
    /// </summary>
    public static class ConditionBuilderRegistry
    {
        private static readonly List<IConditionRowBuilder> _builders = new List<IConditionRowBuilder>();
        private static bool _initialized = false;

        public static IReadOnlyList<IConditionRowBuilder> Builders
        {
            get
            {
                EnsureInitialized();
                return _builders;
            }
        }

        public static List<String> DisplayNames
        {
            get
            {
                EnsureInitialized();
                List<string> names = new List<string>(_builders.Count);
                for (int i = 0; i < _builders.Count; i++)
                {
                    names.Add(_builders[i].DisplayName);
                }

                return names;
            }
        }

        /// <summary>
        /// Retrieves the condition row builder at the specified index from the registry if it exists
        /// </summary>
        /// <param name="index">The zero-based index of the desired condition row builder</param>
        /// <return>The condition row builder at the specified index, or null if the index is out of range</return>
        public static IConditionRowBuilder GetBuilder(int index)
        {
            EnsureInitialized();

            // Exit case - the index is out of range
            if(index < 0 || index >= _builders.Count) return null;
            
            return _builders[index];
        }

        /// <summary>
        /// Registers a new condition row builder into the registry
        /// </summary>
        /// <param name="builder">The condition row builder to be registered</param>
        public static void Register(IConditionRowBuilder builder)
        {
            // Exit case - the builder being registered doesn't exist
            if (builder == null) return;
            
            // Exit case - the builder is already registered
            if (_builders.Contains(builder)) return;
            _builders.Add(builder);
        }

        /// <summary>
        /// Ensures that the condition builder registry has been properly initialized by setting up
        /// default builders if no initialization has occurred
        /// </summary>
        private static void EnsureInitialized()
        {
            // Exit case - already initialized
            if (_initialized) return;

            _initialized = true;
            
            // Register default builders
            Register(new TraitConditionBuilder());
            Register(new RelationshipConditionBuilder());
        }
    }
}