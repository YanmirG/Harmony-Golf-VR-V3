using Convai.Scripts.Runtime.Features;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.NarrativeDesign
{
    [CustomPropertyDrawer(typeof(SectionChangeEventsData))]
    public class NarrativeDesignSectionChangeEventsDataPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sectionIdProperty = property.FindPropertyRelative("id");
            SerializedProperty onSectionStartProperty = property.FindPropertyRelative("onSectionStart");
            SerializedProperty onSectionEndProperty = property.FindPropertyRelative("onSectionEnd");

            Rect sectionIdRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(sectionIdRect, "Section ID", sectionIdProperty.stringValue);

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(position, onSectionStartProperty, true);
            position.y += EditorGUI.GetPropertyHeight(onSectionStartProperty) + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(position, onSectionEndProperty, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty onSectionStartProperty = property.FindPropertyRelative("onSectionStart");
            SerializedProperty onSectionEndProperty = property.FindPropertyRelative("onSectionEnd");

            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(onSectionStartProperty) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(onSectionEndProperty);

            return height;
        }
    }
}