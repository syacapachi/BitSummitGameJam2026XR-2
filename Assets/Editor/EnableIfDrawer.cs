#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Linq;
    using System;
    using Syacapachi.Attribute;
    using static UnityEditor.Rendering.FilterWindow;
    using static UnityEngine.Rendering.VolumeComponent;

    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // メインスレッドで安全に描画できるか確認
            if (!IsMainThreadSafe())
            {
                // メインスレッドに戻したときに再描画するよう登録しておく
                EditorApplication.delayCall += () => SafeRepaint(property);
                return;
            }

            EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
            bool enabled = EvaluateConditionsRecursive(property, enableIf);

            bool prevGUIEnabled = GUI.enabled;
            GUI.enabled = enabled;

            if (!enableIf.hideWhenFalse || enabled)
            {
                try
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
                catch (UnityException e)
                {
                    // フォント等がメインスレッド依存で失敗するケースを安全に握りつぶす
                    Debug.LogWarning($"[EnableIfDrawer] PropertyField skipped due to UnityException: {e.Message}");
                }
                catch (ExitGUIException)
                {
                    // これはUnityの正常動作なのでそのまま投げ直す
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EnableIfDrawer] Unexpected error drawing property: {ex}");
                }
            }

            GUI.enabled = prevGUIEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!IsMainThreadSafe())
                return 0f;

            EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
            bool enabled = EvaluateConditionsRecursive(property, enableIf);
            if (enableIf.hideWhenFalse && !enabled)
                return 0f;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        // --- 以下は既存のネスト対応ロジック（そのまま） ---
        private bool EvaluateConditionsRecursive(SerializedProperty property, EnableIfAttribute attribute)
        {
            object targetObject = GetParentTarget(property) ?? property.serializedObject.targetObject;
            if (targetObject == null)
                return true;

            bool[] results = attribute.conditionFieldNames.Select(name =>
            {
                bool negate = name.StartsWith("!");
                string fieldName = negate ? name.Substring(1) : name;

                FieldInfo field = GetFieldRecursiveFromRoot(targetObject, fieldName);
                if (field == null)
                    return false;

                object value = field.GetValue(GetObjectContainingField(property, targetObject));
                bool result = value switch
                {
                    bool b => b,
                    Enum e => Convert.ToInt32(e) != 0,
                    _ => value != null
                };

                return negate ? !result : result;
            }).ToArray();

            return attribute.logic switch
            {
                ConditionLogic.AND => results.All(r => r),
                ConditionLogic.OR => results.Any(r => r),
                ConditionLogic.NOT => !results.FirstOrDefault(),
                ConditionLogic.NAND => !results.All(r => r),
                ConditionLogic.NOR => !results.Any(r => r),
                ConditionLogic.XOR => results.Count(r => r) == 1,
                _ => true,
            };
        }

        // 再帰的に型情報から FieldInfo を探す（継承チェーン対応）
        private FieldInfo GetFieldRecursiveFromRoot(object obj, string fieldName)
        {
            if (obj == null) return null;
            Type type = obj.GetType();
            while (type != null)
            {
                var f = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) return f;
                type = type.BaseType;
            }
            return null;
        }

        // property.propertyPath をたどって「そのフィールドを含むオブジェクト」を返す
        private object GetParentTarget(SerializedProperty property)
        {
            string path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            for (int i = 0; i < elements.Length - 1; i++)
            {
                string element = elements[i];
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = int.Parse(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValueFromObject(obj, elementName, index);
                }
                else
                {
                    obj = GetValueFromObject(obj, element);
                }
                if (obj == null) return null;
            }

            return obj;
        }


        private object GetValueFromObject(object source, string name, int index = -1)
        {
            if (source == null) return null;
            FieldInfo f = GetFieldRecursiveFromRoot(source, name);
            if (f == null) return null;
            object v = f.GetValue(source);
            if (index >= 0 && v is System.Collections.IEnumerable enumerable)
            {
                var en = enumerable.GetEnumerator();
                for (int i = 0; i <= index; i++)
                {
                    if (!en.MoveNext()) return null;
                }

                return en.Current;
            }
            return v;
        }

        // このプロパティが属する「そのオブジェクト実体」（フィールドをもつインスタンス）を返す補助
        private object GetObjectContainingField(SerializedProperty property, object root)
        {
            // GetParentTarget で得たものが root のはずなのでそのまま返す（冗長だが将来の拡張用）
            return GetParentTarget(property) ?? root;
        }

        // --- メインスレッド判定と安全な再描画 ---
        private bool IsMainThreadSafe()
        {
            try
            {
                // main-thread-only APIs throw if called from loading thread; Screen.width is safe to probe
                var _ = UnityEngine.Screen.width;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SafeRepaint(SerializedProperty property)
        {
            try
            {
                if (property == null || property.serializedObject == null) return;
                // Repaint inspector of the target object
                var editors = UnityEditor.Editor.CreateEditor(property.serializedObject.targetObject);
                if (editors != null) editors.Repaint();
            }
            catch { /* swallow */ }
        }
    }
}
#endif
