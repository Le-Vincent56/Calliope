using System.Text;
using UnityEditor;
using UnityEngine;

namespace Calliope.Editor.SceneTemplateEditor
{
    /// <summary>
    /// Helper class for saving and loading node positions in the scene template editor;
    /// Users can manually clear via Edit > Clear All Preferences if needed
    /// </summary>
    public static class NodePositionStorage
    {
        private const string PositionPrefix = "Calliope_NodePos_";

        /// <summary>
        /// Saves the position of a node in the scene template editor to persistent storage
        /// </summary>
        /// <param name="templateID">The unique identifier of the template containing the node</param>
        /// <param name="beatID">The unique identifier of the specific node within the template</param>
        /// <param name="position">The position of the node to be saved, represented as a 2D vector</param>
        public static void SavePosition(string templateID, string beatID, Vector2 position)
        {
            // Build the key
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.Append(PositionPrefix);
            keyBuilder.Append(templateID);
            keyBuilder.Append("_");
            keyBuilder.Append(beatID);
            
            // Build the position
            StringBuilder positionBuilder = new StringBuilder();
            positionBuilder.Append(position.x);
            positionBuilder.Append(",");
            positionBuilder.Append(position.y);
            
            // Save the position
            EditorPrefs.SetString(keyBuilder.ToString(), positionBuilder.ToString());
        }

        /// <summary>
        /// Loads the position of a node from persistent storage or returns the default position if no data is found
        /// </summary>
        /// <param name="templateID">The unique identifier of the template containing the node</param>
        /// <param name="beatID">The unique identifier of the specific node within the template</param>
        /// <param name="defaultPosition">The default position to return if the node's position has not been saved</param>
        /// <returns>The position of the node as a 2D vector. Returns the default position if no saved position was found or if saved data is invalid</returns>
        public static Vector2 LoadPosition(string templateID, string beatID, Vector2 defaultPosition)
        {
            // Build the key
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.Append(PositionPrefix);
            keyBuilder.Append(templateID);
            keyBuilder.Append("_");
            keyBuilder.Append(beatID);
            string key = keyBuilder.ToString();

            // Exit case - the position has not been saved yet
            if (!EditorPrefs.HasKey(key)) return defaultPosition;
            
            string positionString = EditorPrefs.GetString(key);
            string[] parts = positionString.Split(',');

            // Exit case - the position string is invalid
            if (parts.Length != 2) return defaultPosition;

            // Exit cases - unable to parse the position
            if (!float.TryParse(parts[0], out float x)) return defaultPosition;
            if (!float.TryParse(parts[1], out float y)) return defaultPosition;
            
            return new Vector2(x, y);
        }

        /// <summary>
        /// Deletes the stored position of a node in the scene template editor from persistent storage
        /// </summary>
        /// <param name="templateID">The unique identifier of the template containing the node</param>
        /// <param name="beatID">The unique identifier of the specific node within the template</param>
        public static void DeletePosition(string templateID, string beatID)
        {
            // Build the key
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.Append(PositionPrefix);
            keyBuilder.Append(templateID);
            keyBuilder.Append("_");
            keyBuilder.Append(beatID);
            string key = keyBuilder.ToString();
            
            // Exit case - the key does not exist in the EditorPrefs
            if(!EditorPrefs.HasKey(key)) return;

            // Delete the key
            EditorPrefs.DeleteKey(key);
        }
    }
}