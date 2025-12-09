using Calliope.Core.ValueObjects;
using UnityEditor;
using UnityEngine;

namespace Calliope.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ContextModifier))]
    public class ContextModifierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            float keyWidth = position.width * 0.35f;
            float opWidth = position.width * 0.35f;
            float valueWidth = position.width * 0.25f;

            Rect keyRect = new Rect(
                position.x, position.y,
                keyWidth - spacing, lineHeight
           );
            Rect opRect = new Rect(
                position.x + keyWidth,
                position.y, opWidth - spacing, lineHeight
            );
            Rect valueRect = new Rect(
                position.x + keyWidth + opWidth, 
                position.y, valueWidth, lineHeight
            );

            // Draw fields
            SerializedProperty keyProp = property.FindPropertyRelative("Key");
            SerializedProperty opProp = property.FindPropertyRelative("Operation");
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
            EditorGUI.PropertyField(opRect, opProp, GUIContent.none);

            // Only show value field for operations that use it
            ContextModifierOperation op = (ContextModifierOperation)opProp.enumValueIndex;
            if (op != ContextModifierOperation.SetTrue && op != ContextModifierOperation.SetFalse)
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
            else
                EditorGUI.LabelField(valueRect, "(n/a)");

            EditorGUI.EndProperty();
        }
    }
}
