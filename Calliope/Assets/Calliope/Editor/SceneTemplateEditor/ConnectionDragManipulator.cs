using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Handles dragging to create connections between beat nodes
    /// </summary>
    public class ConnectionDragManipulator : MouseManipulator
    {
        private readonly BeatNodeView _sourceNode;
        private readonly SceneTemplateEditorWindow _window;
        private VisualElement _previewLine;
        private VisualElement _highlightedPort;
        private bool _isDragging;

        public ConnectionDragManipulator(BeatNodeView sourceNode, SceneTemplateEditorWindow window)
        {
            _sourceNode = sourceNode;
            _window = window;
            _isDragging = false;
        }

        /// <summary>
        /// Registers mouse event callbacks on the target element to enable dragging functionality
        /// for creating connections between beat nodes
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <summary>
        /// Unregisters previously registered mouse event callbacks from the target element
        /// to disable dragging functionality for creating connections between beat nodes
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <summary>
        /// Handles the mouse down event to initiate the process of creating a connection
        /// between beat nodes by starting the drag operation and creating a preview line
        /// </summary>
        /// <param name="evt">The mouse down event containing information about the mouse button pressed and event position</param>
        private void OnMouseDown(MouseDownEvent evt)
        {
            // Exit case - not the left mouse button
            if (evt.button != 0) return;

            _isDragging = true;
            
            // Create the preview line
            _previewLine = new ConnectionPreviewLine(_sourceNode);
            _window.GetGraphView().Add(_previewLine);

            target.CaptureMouse();
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles mouse movement events to dynamically update the position of the preview connection line
        /// during a drag operation for creating connections between beat nodes
        /// </summary>
        /// <param name="evt">The event data for the mouse movement, including current cursor position</param>
        private void OnMouseMove(MouseMoveEvent evt)
        {
            // Exit case - not dragging or no preview line is set
            if (!_isDragging || _previewLine == null) return;
            
            // Update the preview line
            if (_previewLine is ConnectionPreviewLine preview)
            {
                Vector2 mousePos = _window.GetGraphView().WorldToLocal(evt.mousePosition);
                preview.UpdateEndPosition(mousePos);
            }

            UpdatePortHighlight(evt.mousePosition);
            
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the mouse up event to finalize the drag operation for creating a connection
        /// between beat nodes; stops the drag, removes the preview line, and attempts to
        /// create a connection if the user releases the mouse over a valid target node
        /// </summary>
        /// <param name="evt">
        /// The mouse up event containing information about the release action,
        /// such as the position and button used
        /// </param>
        private void OnMouseUp(MouseUpEvent evt)
        {
            // Exit case - not dragging
            if (!_isDragging) return;

            // Exit case - not the left mouse button
            if (evt.button != 0) return;

            _isDragging = false;
            target.ReleaseMouse();
            
            // Remove preview line
            if (_previewLine != null)
            {
                _previewLine.RemoveFromHierarchy();
                _previewLine = null;
            }
            
            // Clear any port highlighting
            ClearPortHighlight();
            
            // Check if the user released over a valid target node
            Vector2 mousePos = evt.mousePosition;
            VisualElement targetElement = _window.GetGraphView().panel.Pick(mousePos);
            
            // Walk up the hierarchy to find a BeatNodeView
            BeatNodeView targetNode = null;
            VisualElement current = targetElement;
            while (current != null)
            {
                if (current is BeatNodeView node)
                {
                    targetNode = node;
                    break;
                }
                current = current.parent;
            }
            
            // Create a connection if a valid target was found
            if (targetNode != null && targetNode != _sourceNode)
                _window.CreateBranch(_sourceNode, targetNode);
            
            evt.StopPropagation();
        }

        /// <summary>
        /// Updates the visual highlight state of a port based on the current mouse position;
        /// highlights the input port of a beat node if the mouse is hovering over a compatible target node;
        /// Clears any existing port highlight if no valid node is detected
        /// </summary>
        /// <param name="mousePosition">The current position of the mouse in world coordinates</param>
        private void UpdatePortHighlight(Vector2 mousePosition)
        {
            VisualElement targetElement = _window.GetGraphView().panel.Pick(mousePosition);
            
            // Find if we're hovering over a node
            BeatNodeView targetNode = null;
            VisualElement current = targetElement;
            while (current != null)
            {
                if (current is BeatNodeView node && node != _sourceNode)
                {
                    targetNode = node;
                    break;
                }
                current = current.parent;
            }
            
            // If hovering over a different node, highlight its input port
            if (targetNode != null && targetNode.InputPort != _highlightedPort)
            {
                ClearPortHighlight();
                _highlightedPort = targetNode.InputPort;
                _highlightedPort?.AddToClassList("node-port-highlight");
            } else if (targetNode == null)
            {
                ClearPortHighlight();
            }
        }

        /// <summary>
        /// Removes any current highlight effect applied to a node port, ensuring
        /// that previously highlighted ports no longer display visual indicators
        /// of selection or interaction
        /// </summary>
        private void ClearPortHighlight()
        {
            // Exit case - no port is highlighted
            if (_highlightedPort == null) return;
            
            // Remove the port highlight
            _highlightedPort.RemoveFromClassList("node-port-highlight");
            _highlightedPort = null;
        }
    }
}