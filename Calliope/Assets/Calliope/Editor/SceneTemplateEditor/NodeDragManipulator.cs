using UnityEngine;
using UnityEngine.UIElements;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Handles dragging behavior for beat nodes in the scene template editor
    /// </summary>
    public class NodeDragManipulator : MouseManipulator
    {
        private SceneTemplateEditorWindow _window;
        private Vector2 _startMousePosition;
        private Vector2 _startNodePosition;
        private bool _isDragging;
        private readonly BeatNodeView _nodeView;

        public NodeDragManipulator(BeatNodeView nodeView, SceneTemplateEditorWindow window)
        {
            _nodeView = nodeView;
            _window = window;
            _isDragging = false;
        }

        /// <summary>
        /// Registers mouse event callbacks for drag handling
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <summary>
        /// Unregisters mouse event callbacks
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <summary>
        /// Handles the mouse down event to initiate dragging behavior for a beat node
        /// </summary>
        /// <param name="evt">The mouse down event containing information about the mouse press</param>
        private void OnMouseDown(MouseDownEvent evt)
        {
            // Exit case - the left mouse button wasn't pressed
            if (evt.button != 0) return;
            
            // Store starting positions
            _startMousePosition = evt.localMousePosition;
            _startNodePosition = _nodeView.Position;
            _isDragging = true;
            
            // Capture mouse to receive events even when outside the element
            target.CaptureMouse();
            
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles mouse movement events to update the position of the beat node while dragging
        /// </summary>
        /// <param name="evt">The mouse movement event containing the current mouse position</param>
        private void OnMouseMove(MouseMoveEvent evt)
        {
            // Exit case - not currently dragging
            if (!_isDragging) return;
            
            // Get the GraphView element
            VisualElement graphView = target.parent;
            
            // Convert mouse position to GraphView coordinates
            Vector2 mousePositionInGraph = graphView.WorldToLocal(evt.mousePosition);
            
            // Calculate offset from where the user grabbed the node
            Vector2 newPosition = mousePositionInGraph - _startMousePosition;
            
            // Update the node position
            _nodeView.SetPosition(newPosition);
            
            // Update connections
            _window?.UpdateConnections();
            
            evt.StopPropagation();
        }

        /// <summary>
        /// Handles the mouse release event to finalize the drag operation of a beat node
        /// </summary>
        /// <param name="evt">The mouse event data associated with the mouse release operation</param>
        private void OnMouseUp(MouseUpEvent evt)
        {
            // Exit case - not currently dragging
            if (!_isDragging) return;
            
            // Exit case - the left mouse button wasn't released
            if (evt.button != 0) return;

            _isDragging = false;
            
            // Release mouse capture
            target.ReleaseMouse();
            
            // Save the new position
            _window?.SaveNodePositions();

            evt.StopPropagation();
        }
    }
}