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
    }
}