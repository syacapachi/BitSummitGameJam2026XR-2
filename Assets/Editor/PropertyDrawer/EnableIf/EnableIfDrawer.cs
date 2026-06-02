#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
            bool enabled = EnableIfEvaluator.EvaluateConditionsRecursive(property, enableIf);

            if (!enableIf.hideWhenFalse || enabled)
            {
                // 条件が満たされていない場合はGUIを無効化して描画するスコープを生成(スコープを抜けると以前のGUI状態に戻る)
                using (new EditorGUI.DisabledScope(!enabled))
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
            bool enabled = EnableIfEvaluator.EvaluateConditionsRecursive(property, enableIf);

            if (!enableIf.hideWhenFalse || enabled)
                return EditorGUI.GetPropertyHeight(property, label, true);
            //表示しない場合は隠すために高さ0を返す
            return 0f;
        }
    }
}
#endif
