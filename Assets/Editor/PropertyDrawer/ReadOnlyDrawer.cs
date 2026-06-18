#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ReadOnlyAttribute attr = (ReadOnlyAttribute)attribute;
            if (attr.ShowOnlyPlayMode && !Application.isPlaying) return;
            // ここでDisabledScopeを使用して、プロパティを読み取り専用にします。
            //スコープを抜けると、プロパティは元に戻ります。
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }
}
#endif
