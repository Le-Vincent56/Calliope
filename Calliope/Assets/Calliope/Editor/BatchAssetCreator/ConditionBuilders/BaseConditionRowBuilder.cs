using System;
using Calliope.Editor.BatchAssetCreator.RowData.Conditions;
using Calliope.Editor.BatchAssetCreator.Tabs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.BatchAssetCreator.ConditionBuilders
{
    /// <summary>
    /// Provides a base implementation for creating and managing condition rows within the batch asset creation workflow;
    /// derive from this abstract class to implement specific condition logic and row behavior
    /// </summary>
    public abstract class BaseConditionRowBuilder : IConditionRowBuilder
    {
        private static readonly Color SeparatorColor = new Color(0.4f, 0.4f, 0.4f);
        
        private const float SeparatorWidth = 1f;
        private const float CellPadding = 8f;
        
        public abstract string DisplayName { get; }
        public abstract ColumnDefinition[] Columns { get; }
        public abstract string AssetTypeName { get; }

        public abstract BaseConditionRowData CreateRowData();
        public abstract void BuildRowFields(VisualElement container, BaseConditionRowData data, Action onDataChanged);
        public abstract bool CreateAsset(BaseConditionRowData data, string folderPath);

        /// <summary>
        /// Creates a table cell element by configuring its size and layout and adding the provided content as a child
        /// </summary>
        /// <param name="columnIndex">The index of the column for which the cell is being created, used to determine its size and flex properties based on the column definitions</param>
        /// <param name="content">The visual element to be added as the content of the cell</param>
        /// <returns>A configured <see cref="VisualElement"/> representing the cell with the provided content</returns>
        protected VisualElement CreateCell(int columnIndex, VisualElement content)
        {
            ColumnDefinition column = Columns[columnIndex];

            // Create the cell
            VisualElement cell = new VisualElement();
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;

            // Decide cell width
            if (column.Width.HasValue)
            {
                cell.style.width = column.Width.Value;
            }
            else
            {
                cell.style.flexGrow = column.FlexGrow > 0 
                    ? column.FlexGrow 
                    : 1;
                cell.style.flexShrink = 1;
                cell.style.minWidth = 0;
            }

            content.style.flexGrow = 1;
            content.style.overflow = Overflow.Hidden;
            content.style.flexShrink = 1;
            content.style.minWidth = 0;
            cell.Add(content);

            return cell;
        }

        /// <summary>
        /// Creates a visual separator element with a predefined width, height, color, and padding to be used for dividing content in the UI layout
        /// </summary>
        /// <returns>A configured <see cref="VisualElement"/> representing the separator</returns>
        protected VisualElement CreateSeparator()
        {
            VisualElement separator = new VisualElement();
            separator.style.width = SeparatorWidth;
            separator.style.height = 20;
            separator.style.backgroundColor = SeparatorColor;
            separator.style.marginLeft = CellPadding;
            separator.style.marginRight = CellPadding;
            return separator;
        }
    }
}