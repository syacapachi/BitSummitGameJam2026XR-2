#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(EnableIfEnumAttribute))]
    public class EnableIfEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnableIfEnumAttribute condition = (EnableIfEnumAttribute)attribute;
            //string conditionPath = property.propertyPath.Replace(property.name, condition.enumFiledName);
            //// Enumを参照するプロパティを検索
            //SerializedProperty enumProp = property.serializedObject.FindProperty(conditionPath);
            //bool isValidEnum = enumProp != null && IsEnumValueValid(enumProp, condition.enumValues);
            bool isValidEnum = EnableIfEvaluator.EvaluateEnumConditionRecursive(property, condition);

            // hideWhenFalse が true の場合、非表示にする
            if (!isValidEnum && condition.hideWhenFalse)
                return;

            // 条件が満たされていない場合はGUIを無効化して描画するスコープを生成(スコープを抜けると以前のGUI状態に戻る)
            using (new EditorGUI.DisabledScope(!isValidEnum))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnableIfEnumAttribute condition = (EnableIfEnumAttribute)attribute;
            //string conditionPath = property.propertyPath.Replace(property.name, condition.enumFiledName);
            //// Enumを参照するプロパティを検索
            //SerializedProperty enumProp = property.serializedObject.FindProperty(conditionPath);
            //bool isValidEnum = enumProp != null && IsEnumValueValid(enumProp, condition.enumValues);
            bool isValidEnum = EnableIfEvaluator.EvaluateEnumConditionRecursive(property, condition);
            if (!condition.hideWhenFalse || isValidEnum)
                return EditorGUI.GetPropertyHeight(property, label, true);
            //表示しない場合は隠すために高さ0を返す
            return 0f;
        }
    }
}
#endif
