using Calliope.Core.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Draws a visual connection line between two beat nodes
    /// </summary>
    public class BeatConnectionView : VisualElement
    {
        private SceneTemplateEditorWindow _window;
        private readonly BeatNodeView _fromNode;
        private readonly BeatNodeView _toNode;
        private readonly IBeatBranch _branch;

        private Color _lineColor;
        private float _lineWidth;
        private bool _isHovered;

        public BeatNodeView FromNode => _fromNode;
        public BeatNodeView ToNode => _toNode;
        public IBeatBranch Branch => _branch;

        public BeatConnectionView(BeatNodeView fromNode, BeatNodeView toNode, IBeatBranch branch, SceneTemplateEditorWindow window)
        {
            _fromNode = fromNode;
            _toNode = toNode;
            _branch = branch;
            _window = window;
            
            // Default styling
            _lineColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            _lineWidth = 2f;
            
            // Check if this is a conditional branch
            if (_branch is { Conditions: { Count: > 0 } })
            {
                // Conditional branches are blue
                _lineColor = new Color(0.3f, 0.7f, 1f, 0.9f);
                _lineWidth = 3f;
            }
            
            // Make this element draw on top of the graph but below the nodes
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.width = 10000;
            style.height = 10000;
            
            // Set picking mode so it doesn't interfere with node clicks
            pickingMode = PickingMode.Position;
            
            // Register for custom drawing
            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>
        /// Generates the visual content for the current element, including drawing a bezier connection line
        /// between two beat node
        /// </summary>
        /// <param name="context">The mesh generation context used to construct the visual content</param>
        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            // Exit case - invalid nodes
            if (_fromNode == null || _toNode == null) return;
            
            // Calculate connection points
            Vector2 start = GetOutputPoint(_fromNode);
            Vector2 end = GetInputPoint(_toNode);
            
            // Draw the bezier curve
            DrawBezier(context, start, end, _lineColor, _lineWidth);
        }

        /// <summary>
        /// Calculates the output connection point for a given beat node
        /// </summary>
        /// <param name="node">The target beat node for which the output connection point is calculated</param>
        /// <returns>A vector representing the position of the output connection point</returns>
        private Vector2 GetOutputPoint(BeatNodeView node)
        {
            // Calculate the position based on the node's position and size
            float headerHeight = 40f;
            return new Vector2(
                node.Position.x + node.resolvedStyle.width - 8f,
                node.Position.y + (headerHeight * 0.5f)
            );
        }

        /// <summary>
        /// Calculates the input connection point for a given beat node
        /// </summary>
        /// <param name="node">The target beat node for which the input connection point is calculated</param>
        /// <returns>A vector representing the position of the input connection point</returns>
        private Vector2 GetInputPoint(BeatNodeView node)
        {
            // Calculate the position based on the node's position and size
            float headerHeight = 40f;
            return new Vector2(
                node.Position.x + 8f,
                node.Position.y + (headerHeight * 0.5f)
            );
        }

        /// <summary>
        /// Draws a quadratic Bezier curve between two points with optional styling
        /// </summary>
        /// <param name="context">The mesh generation context used for rendering the curve</param>
        /// <param name="start">The starting point of the Bezier curve</param>
        /// <param name="end">The ending point of the Bezier curve</param>
        /// <param name="color">The color of the Bezier curve</param>
        /// <param name="width">The thickness of the Bezier curve</param>
        private void DrawBezier(MeshGenerationContext context, Vector2 start, Vector2 end, Color color, float width)
        {
            // Calculate the control points for the bezier curve
            float distance = Mathf.Abs(end.x - start.x);
            float tangentOffset = Mathf.Min(distance * 0.5f, 100f);
            Vector2 startTangent = start + Vector2.right * tangentOffset;
            Vector2 endTangent = end + Vector2.left * tangentOffset;
            
            // Create a painter for drawing
            Painter2D painter = context.painter2D;
            
            // Draw a thicker invisible hit area first
            painter.strokeColor = new Color(0, 0, 0, 0.01f);
            painter.lineWidth = 5f;
            painter.lineCap = LineCap.Round;

            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(startTangent, endTangent, end);
            painter.Stroke();

            // Draw the actual visible line
            Color lineColor = _isHovered ? Color.white : color;
            float lineWidth = _isHovered ? width + 1f : width;
            
            painter.strokeColor = lineColor;
            painter.lineWidth = lineWidth;
            painter.lineCap = LineCap.Round;
            
            // Draw the bezier curve
            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(startTangent, endTangent, end);
            painter.Stroke();
            
            // Draw an arrowhead at the end
            DrawArrowhead(painter, endTangent, end, lineColor, lineWidth);
        }

        /// <summary>
        /// Draws an arrowhead at the specified endpoint of a line, oriented based on the tangent vector;
        /// this method is typically used to signify the direction of a connection in a visual graph
        /// </summary>
        /// <param name="painter">The painter object used for rendering the arrowhead</param>
        /// <param name="tangent">The tangent vector indicating the direction of the line at the endpoint</param>
        /// <param name="end">The endpoint where the arrowhead is to be drawn</param>
        /// <param name="color">The color of the arrowhead</param>
        /// <param name="width">The thickness of the line, used to scale the size of the arrowhead</param>
        private void DrawArrowhead(Painter2D painter, Vector2 tangent, Vector2 end, Color color, float width)
        {
            // Calculate the arrow direction
            Vector2 direction = (end - tangent).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            // Calculate the arrow size
            float arrowSize = 10f * width;
            float arrowWidth = 6f + (width * 0.5f);
            
            // Calculate arrow points
            Vector2 arrowTip = end;
            Vector2 arrowLeft = end - (direction * arrowSize) + (perpendicular * arrowWidth);
            Vector2 arrowRight = end - (direction * arrowSize) - (perpendicular * arrowWidth);
            
            // Draw filled triangle
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(arrowTip);
            painter.LineTo(arrowLeft);
            painter.LineTo(arrowRight);
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>
        /// Updates the visual representation of the connection line between
        /// the "FromNode" and "ToNode" within the editor environment; this method
        /// triggers a redraw of the connection to reflect any changes, such as modifications
        /// to node positions or styling
        /// </summary>
        public void UpdateConnection()
        {
            MarkDirtyRepaint();
            panel?.visualTree.MarkDirtyRepaint();
        }

        /// <summary>
        /// Updates the hover state of the connection and triggers a repaint if the state changes
        /// </summary>
        /// <param name="hovered">True if the connection is being hovered over, false otherwise</param>
        public void SetHovered(bool hovered)
        {
            // Exit case - no change in hover state
            if (_isHovered == hovered) return;
            
            // Set the hover state and repaint
            _isHovered = hovered;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Calculates the shortest distance from a given point to the bezier curve connecting two beat nodes
        /// </summary>
        /// <param name="point">The point to calculate the distance to</param>
        /// <returns>The shortest distance from the point to the bezier curve</returns>
        public float GetDistanceToPoint(Vector2 point)
        {
            Vector2 start = GetOutputPoint(_fromNode);
            Vector2 end = GetInputPoint(_toNode);
            
            // Calculate bezier control points
            float distance = Mathf.Abs(end.x - start.x);
            float tangentOffset = Mathf.Min(distance * 0.5f, 100f);
            Vector2 startTangent = start + Vector2.right * tangentOffset;
            Vector2 endTangent = end + Vector2.left * tangentOffset;
            
            // Sample points along the curve to find the closest distance
            float minDistance = float.MaxValue;
            int samples = 20;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 pointOnCurve = CalculateBezierPoint(t, start, startTangent, endTangent, end);
                float distanceToPoint = Vector2.Distance(pointOnCurve, point);

                // Skip if the distance is greater than the minimum distance
                if (distanceToPoint >= minDistance) continue;
                
                // Update the minimum distance
                minDistance = distanceToPoint;
            }

            return minDistance;
        }

        /// <summary>
        /// Calculates a point on a cubic Bezier curve based on the specified control points and interpolation value
        /// </summary>
        /// <param name="t">The interpolation value, ranging from 0 to 1, where 0 represents the start of the curve and 1 represents the end</param>
        /// <param name="p0">The starting point of the Bezier curve</param>
        /// <param name="p1">The first control point, which influences the shape of the curve near the start</param>
        /// <param name="p2">The second control point, which influences the shape of the curve near the end</param>
        /// <param name="p3">The ending point of the Bezier curve</param>
        /// <returns>The calculated point on the Bezier curve at the specified interpolation value</returns>
        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        /// <summary>
        /// Deletes the visual and logical connection between two beat nodes;
        /// invokes the `DeleteBranch` method on the associated SceneTemplateEditorWindow
        /// to handle removal of the underlying branch data and asset cleanup
        /// </summary>
        private void OnDeleteConnection() => _window?.DeleteBranch(_fromNode, _toNode, _branch);
    }
}