#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerDrawer : PropertyDrawer
    {
        private static readonly GUIContent warningLabel = new GUIContent("Use with int fields only.","int only");
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                LayerAttribute attr = (LayerAttribute)attribute;
                //これがあると、Undo、RedoPrefab対応
                EditorGUI.BeginProperty(position, label, property);
                int layer = property.intValue;
                int newLayer;
                if (attr.UseMask)
                {
                    newLayer = EditorGUI.MaskField(position, label, layer, InternalEditorUtility.layers);
                }
                else
                {
                    if (layer < 0 || layer > 32) layer = 0;
                    newLayer = EditorGUI.LayerField(position, label, layer);
                }

                if (newLayer != layer)
                {
                    property.intValue = newLayer;
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