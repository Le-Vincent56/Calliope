using System.Text;

namespace Calliope.Editor.BatchAssetCreator.RowData.Conditions
{
    /// <summary>
    /// Represents data for a condition that evaluates context-specific information within a scene
    /// in a batch asset creation workflow
    /// </summary>
    public class SceneContextConditionRowData : BaseConditionRowData
    {
        public string Key = "";
        public int ComparisonIndex = 0;
        public string TargetValue = "";

        public override bool IsValid => !string.IsNullOrEmpty(Key);
        public override bool HasAnyData => !string.IsNullOrEmpty(Key) || !string.IsNullOrEmpty(TargetValue);

        /// <summary>
        /// Creates a new instance of <see cref="SceneContextConditionRowData"/> with the same properties as the current object
        /// </summary>
        /// <returns>A new instance of <see cref="BaseConditionRowData"/> duplicating the current object's state</returns>
        public override BaseConditionRowData Clone()
        {
            return new SceneContextConditionRowData
            {
                Key = Key,
                ComparisonIndex = ComparisonIndex,
                TargetValue = TargetValue
            };
        }

        /// <summary>
        /// Retrieves the identifier associated with the row in the form "Context" followed by the key value with dots replaced by underscores
        /// </summary>
        /// <returns>A string representing the unique row identifier</returns>
        public override string GetRowID()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Context");
            builder.Append(Key.Replace(".", "_"));
            return builder.ToString();
        }
    }
}