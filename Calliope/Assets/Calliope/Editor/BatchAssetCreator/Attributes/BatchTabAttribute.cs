using System;

namespace Calliope.Editor.BatchAssetCreator.Attributes
{
    /// <summary>
    /// Indicates that a class is a batch asset creation tab; used for auto-registration within the Batch Asset Creator
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BatchTabAttribute : Attribute
    {
        /// <summary>
        /// Display order for the tab; lower values appear first;
        /// Built-in tabs use orders 0-100; external tabs should use 100+
        /// </summary>
        public int Order { get; set; } = 100;
        
        public BatchTabAttribute() { }
        
        public BatchTabAttribute(int order) => Order = order;
    }
}