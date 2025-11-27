using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Represents a visual element that renders a preview connection line within the editor;
    /// The connection line is drawn dynamically between a source node and an end position,
    /// typically used during a drag-and-drop interaction to visualize potential connections
    /// between `BeatNodeView` elements
    /// </summary>
    public class ConnectionPreviewLine : VisualElement
    {
        private readonly BeatNodeView _sourceNode;
        private Vector2 _endPosition;

        public ConnectionPreviewLine(BeatNodeView sourceNode)
        {
            _sourceNode = sourceNode;
            _endPosition = GetOutputPoint(sourceNode);

            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;
        }

        public void UpdateEndPosition(Vector2 position)
        {
            _endPosition = position;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Handles the process of generating the visual content for this VisualElement;
        /// it uses a Painter2D to render a dashed bezier curve that represents
        /// the connection preview line between the source node and the end position
        /// </summary>
        /// <param name="context">The MeshGenerationContext used to draw the visual content</param>
        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Vector2 start = GetOutputPoint(_sourceNode);
            
            // Draw dashed line
            Painter2D painter = context.painter2D;
            painter.strokeColor = new Color(1f, 1f, 1f, 0.5f);
            painter.lineWidth = 2f;
            
            // Calculate bezier control points
            float distance = Mathf.Abs(_endPosition.x - start.x);
            float tangentOffset = Mathf.Min(distance * 0.5f, 100f);
            Vector2 startTangent = start + Vector2.right * tangentOffset;
            Vector2 endTangent = _endPosition + Vector2.left * tangentOffset;

            // Draw the line
            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(startTangent, endTangent, _endPosition);
            painter.Stroke();
        }

        /// <summary>
        /// Calculates and returns the output point position for a given BeatNodeView;
        /// the output point represents the location where the connection line should
        /// start for the specified node, taking into account the node's size and position
        /// </summary>
        /// <param name="node">The BeatNodeView instance for which the output point is calculated</param>
        /// <returns>A Vector2 representing the output point position in the local coordinate system</returns>
        private Vector2 GetOutputPoint(BeatNodeView node)
        {
            return new Vector2(
                node.Position.x + node.resolvedStyle.width,
                node.Position.y + (node.resolvedStyle.height * 0.5f)
            );
        }
    }
}