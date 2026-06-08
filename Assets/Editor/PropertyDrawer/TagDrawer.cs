#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagDrawer : PropertyDrawer
    {
        private static readonly GUIContent warningLabel = new GUIContent("Use with string fields only.");
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginProperty(position, label, property);
                string newTag = EditorGUI.TagField(position, label, property.stringValue);
                if (newTag != property.stringValue)
                {
                    property.stringValue = newTag;
                }
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label, warningLabel);
            }
        }
    }
}
#endif