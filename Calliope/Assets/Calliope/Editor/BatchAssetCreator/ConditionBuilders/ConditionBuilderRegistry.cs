using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Calliope.Editor.BatchAssetCreator.Attributes;
using UnityEngine;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    /// <summary>
    /// A registry for managing and accessing implementations of <see cref="IConditionRowBuilder"/>;
    /// Uses reflection to find all classes marked with the [ConditionBuilder] attribute
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
        /// Manually registers a new condition row builder into the registry
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
        /// Forces re-discovery of all builders; call this after loading new assemblies
        /// </summary>
        public static void Refresh()
        {
            _initialized = false;
            _builders.Clear();
            EnsureInitialized();
        }

        /// <summary>
        /// Ensures that the registry is initialized by detecting and registering all available
        /// implementations of <see cref="IConditionRowBuilder"/>;
        /// performs reflection to find all types in loaded assemblies decorated with the [ConditionBuilder] attribute,
        /// orders them by the specified attribute order and displays their display names;
        /// if already initialized, this method has no effect
        /// </summary>
        private static void EnsureInitialized()
        {
            List<(IConditionRowBuilder builder, int order)> discovered = new List<(IConditionRowBuilder, int)>();
            
            // Scan all loaded assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                
                // Skip system assemblies for performance
                string assemblyName = assembly.GetName().Name;

                if (assemblyName.StartsWith("System") || 
                    assemblyName.StartsWith("Unity") ||
                    assemblyName.StartsWith("mscorlib"))
                    continue;

                try
                {
                    // Get all the assembly types
                    Type[] types = assembly.GetTypes();

                    for (int j = 0; j < types.Length; j++)
                    {
                        Type type = types[j];

                        // Skip abstract classes and interfaces
                        if (type.IsAbstract || type.IsInterface) continue;

                        // Check for the ConditionBuilder attribute
                        ConditionBuilderAttribute attribute = type.GetCustomAttribute<ConditionBuilderAttribute>();

                        // Skip if the type does not contain the ConditionBuilder attribute
                        if (attribute == null) continue;

                        if (!typeof(IConditionRowBuilder).IsAssignableFrom(type))
                        {
                            // Build the warning message
                            StringBuilder warningBuilder = new StringBuilder();
                            warningBuilder.Append("[ConditionBuilderRegistry] Type '");
                            warningBuilder.Append(type.FullName);
                            warningBuilder.Append(
                                "' has [ConditionBuilder] attribute but does not implement IConditionRowBuilder");

                            Debug.LogWarning(warningBuilder.ToString());
                            continue;
                        }

                        // Try to create the instance
                        try
                        {
                            IConditionRowBuilder builder = (IConditionRowBuilder)Activator.CreateInstance(type);
                            discovered.Add((builder, attribute.Order));
                        }
                        catch (Exception ex)
                        {
                            // Build the error message
                            StringBuilder errorBuilder = new StringBuilder();
                            errorBuilder.Append("[ConditionBuilderRegistry] Failed to instantiate '");
                            errorBuilder.Append(type.FullName);
                            errorBuilder.Append("': ");
                            errorBuilder.Append(ex.Message);

                            Debug.LogError(errorBuilder.ToString());
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Some assemblies can't be fully loaded - skip them
                }
            }
            
            // Sort by order, then by display name
            discovered.Sort((a, b) =>
            {
                // Get the comparison order
                int orderCompare = a.order.CompareTo(b.order);
                
                // Exit case - if the operation returns and order comparison
                if (orderCompare != 0) return orderCompare;
                
                // Fall back to string comparison
                return string.Compare(a.builder.DisplayName, b.builder.DisplayName, StringComparison.Ordinal);
            });
            
            // Add to the registry
            for (int i = 0; i < discovered.Count; i++)
            {
                _builders.Add(discovered[i].builder);
            }
        }
    }
}