using System;

namespace Calliope.Editor.BatchAssetCreator.Attributes
{
    /// <summary>
    /// Indicates that a class is a condition builder; used for auto-registration within the Batch Asset Creator
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ConditionBuilderAttribute : Attribute
    {
        /// <summary>
        /// Optional display order; lower values appear first (default is 100)
        /// </summary>
        public int Order { get; set; } = 100;
        
        public ConditionBuilderAttribute() { }
        
        public ConditionBuilderAttribute(int order) => Order = order;
    }
}
