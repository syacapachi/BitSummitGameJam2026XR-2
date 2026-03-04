#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(EnableIfEnumAttribute))]
public class EnableIfEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // メインスレッド以外で呼ばれた場合、後で再描画を予約して中断
        if (!IsMainThreadSafe())
        {
            EditorApplication.delayCall += () => SafeRepaint(property);
            return;
        }

        EnableIfEnumAttribute condition = (EnableIfEnumAttribute)attribute;

        // Enumを参照するプロパティを検索
        string conditionPath = property.propertyPath.Replace(property.name, condition.enumFiledName);
        SerializedProperty enumProp = property.serializedObject.FindProperty(conditionPath);

        // 条件プロパティが存在しない場合は通常描画
        if (enumProp == null)
        {
            SafeDrawProperty(position, property, label);
            return;
        }

        bool enabled = false;
        if (enumProp.propertyType == SerializedPropertyType.Enum)
        {
            int currentValue = enumProp.enumValueIndex;
            foreach (int valid in condition.enumValues)
            {
                if (currentValue == valid)
                {
                    enabled = true;
                    break;
                }
            }
        }

        // hideWhenFalse が true の場合、非表示にする
        if (!enabled && condition.hideWhenFalse)
            return;

        // 条件を満たさない場合はグレー表示
        bool prevGUI = GUI.enabled;
        GUI.enabled = enabled;

        SafeDrawProperty(position, property, label);

        GUI.enabled = prevGUI;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 非メインスレッド時は描画スキップ
        if (!IsMainThreadSafe())
            return 0f;

        EnableIfEnumAttribute condition = (EnableIfEnumAttribute)attribute;
        string conditionPath = property.propertyPath.Replace(property.name, condition.enumFiledName);
        SerializedProperty enumProp = property.serializedObject.FindProperty(conditionPath);

        if (enumProp != null && enumProp.propertyType == SerializedPropertyType.Enum)
        {
            bool enabled = false;
            int currentValue = enumProp.enumValueIndex;
            foreach (int valid in condition.enumValues)
            {
                if (currentValue == valid)
                {
                    enabled = true;
                    break;
                }
            }

            if (!enabled && condition.hideWhenFalse)
                return 0f;
        }

        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    // ---------- 共通ユーティリティ ----------

    /// <summary>
    /// 安全に PropertyField を描画（メインスレッド限定）
    /// </summary>
    private void SafeDrawProperty(Rect position, SerializedProperty property, GUIContent label)
    {
        try
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        catch (UnityException e)
        {
            Debug.LogWarning($"[EnableIfEnumDrawer] skipped drawing due to UnityException: {e.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EnableIfEnumDrawer] unexpected error: {ex}");
        }
    }

    /// <summary>
    /// メインスレッドで安全に呼び出せるか判定
    /// </summary>
    private bool IsMainThreadSafe()
    {
        try
        {
            // 一部のAPIはメインスレッド以外で呼ぶとUnityExceptionを投げる
            var _ = UnityEngine.Screen.width;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 後で安全に再描画を要求
    /// </summary>
    private void SafeRepaint(SerializedProperty property)
    {
        try
        {
            if (property == null || property.serializedObject == null)
                return;

            var editors = UnityEditor.Editor.CreateEditor(property.serializedObject.targetObject);
            if (editors != null)
                editors.Repaint();
        }
        catch { /* ignore safely */ }
    }
}
#endif
