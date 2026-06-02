namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using UnityEditor;
    using UnityEngine;
    /// <summary>
    /// EnableIfAttribute の条件を評価するためのユーティリティクラス
    /// </summary>
    public static class EnableIfEvaluator
    {
        /// <summary>
        /// property が属するオブジェクト(Monobehaviour, ScriptableObject, etc.)から条件フィールドを再帰的に探し、条件を評価する
        /// </summary>
        /// <param name="property">評価対象のプロパティ</param>
        /// <param name="attribute">EnableIf 属性</param>
        /// <returns>条件がすべて満たされている場合は true、それ以外は false</returns>
        internal static bool EvaluateConditionsRecursive(SerializedProperty property, EnableIfAttribute attribute)
        {
            // 条件フィールドを探すための「このプロパティが属するオブジェクト(Monobehaviour, ScriptableObject, etc.)」を取得
            // property.serializedObject.targetObject は常に最上位のオブジェクトを指すため、ネストされたプロパティの場合はそこからさらにたどる必要がある
            object targetObject = EditorReflectionCache.GetParentTarget(property) ?? property.serializedObject.targetObject;

            // 条件フィールドを含むオブジェクトを取得（通常は targetObject だが、プロパティのネストによってはさらにたどる必要がある）
            object containingObject = EditorReflectionCache.GetObjectContainingField(property, targetObject);
            if (containingObject == null)
            {
                return true;
            }
            int trueCount = 0; // XOR 用の true の数をカウントする変数
            for (int i = 0; i < attribute.conditionFieldNames.Length; i++)
            {
                string name = attribute.conditionFieldNames[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    Debug.LogWarning($"[EnableIfDrawer] Empty condition field name in EnableIfAttribute on {property.serializedObject.targetObject.GetType().Name}.{property.name}", targetObject as UnityEngine.Object);
                    return true;
                }
                //!で始まる場合は条件を反転させる（例: "!isEnabled" は isEnabled が false のときに有効）
                bool negate = name.Length > 0 && name[0] == '!';
                string fieldName = negate ? name.Substring(1) : name;

                //Unity固有の検索機能を使う方法（ただしプロパティのネストや配列要素のアクセスには対応が難しい)(後遅い)
                //string path = property.propertyPath.Replace(property.name, attribute.name);
                //SerializedProperty prop = property.serializedObject.FindProperty(path);

                //動的アクセス関数のキャッシュにより高速化
                Func<object, object> getter = EditorReflectionCache.GetOrCreateGetter(containingObject.GetType(), fieldName);

                if (getter == null)
                {
                    Debug.LogWarning($"[EnableIfDrawer] Condition field '{fieldName}' not found in {containingObject.GetType().Name}", targetObject as UnityEngine.Object);
                    return true;
                }
                // 条件フィールドの値を取得
                object fieldValue = getter(containingObject);
                bool result = fieldValue switch
                {
                    bool b => b,
                    Enum e => Convert.ToInt64(e) != 0,
                    _ => fieldValue != null
                };

                result = negate ? !result : result;
                switch (attribute.logic)
                {
                    case ConditionLogic.AND:
                        if (!result) return false; // AND なら一つでも false なら全体が false になるので早期リターン
                        break;
                    case ConditionLogic.OR:
                        if (result) return true; // OR なら一つでも true なら全体が true になるので早期リターン
                        break;
                    case ConditionLogic.NOT:
                        return !result; // NOT は単一条件で意味があるはずだが、複数条件があった場合は最初の条件の結果を反転させて返す（他の条件は無視）
                    case ConditionLogic.NAND:
                        if (!result) return true; // NAND は AND の否定なので、一つでも false なら全体が true になる
                        break;
                    case ConditionLogic.NOR:
                        if (result) return false; // NOR は OR の否定なので、一つでも true なら全体が false になる
                        break;
                    case ConditionLogic.XOR:
                    case ConditionLogic.NXOR:
                        if (result)
                            trueCount++;// XOR,NXOR は true によって true になるので、結果は最後にまとめて評価する
                        break;
                }
            }
            return attribute.logic switch
            {
                ConditionLogic.AND => true, // すべての条件が true だった場合にここまで来るので true
                ConditionLogic.NAND => false, // NAND は AND の否定なので、すべての条件が true だった場合にここまで来るので false
                ConditionLogic.NOR => true, // NOR は OR の否定なので、すべての条件が false だった場合にここまで来るので true
                ConditionLogic.XOR => trueCount % 2 == 1, // XOR の結果を評価（true の数が奇数なら true、偶数なら false）
                ConditionLogic.NXOR => trueCount % 2 == 0, // NXOR は XOR の否定なので、true の数が偶数なら true、奇数なら false
                _ => true// NOT はここには来ないはずなのでデフォルトに吸収して true を返す
            };
        }
        internal static bool EvaluateEnumConditionRecursive(SerializedProperty property, EnableIfEnumAttribute attribute)
        {
            // 条件フィールドを探すための「このプロパティが属するオブジェクト(Monobehaviour, ScriptableObject, etc.)」を取得
            // property.serializedObject.targetObject は常に最上位のオブジェクトを指すため、ネストされたプロパティの場合はそこからさらにたどる必要がある
            object targetObject = EditorReflectionCache.GetParentTarget(property) ?? property.serializedObject.targetObject;
            // 条件フィールドを含むオブジェクトを取得（通常は targetObject だが、プロパティのネストによってはさらにたどる必要がある）
            object containingObject = EditorReflectionCache.GetObjectContainingField(property, targetObject);
            if (containingObject == null)
            {
                return true;
            }
            string name = attribute.enumFiledName;
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning($"[EnableIfDrawer] Empty condition field name in EnableIfAttribute on {property.serializedObject.targetObject.GetType().Name}.{property.name}", targetObject as UnityEngine.Object);
                return true;
            }
            //!で始まる場合は条件を反転させる（例: "!isEnabled" は isEnabled が false のときに有効）
            bool negate = name.Length > 0 && name[0] == '!';
            string fieldName = negate ? name.Substring(1) : name;
            //Unity固有の検索機能を使う方法（ただしプロパティのネストや配列要素のアクセスには対応が難しい)(後遅い)
            //string path = property.propertyPath.Replace(property.name, attribute.name);
            //SerializedProperty prop = property.serializedObject.FindProperty(path);
            //動的アクセス関数のキャッシュにより高速化
            Func<object, object> getter = EditorReflectionCache.GetOrCreateGetter(containingObject.GetType(), fieldName);
            if (getter == null)
            {
                Debug.LogWarning($"[EnableIfDrawer] Condition field '{fieldName}' not found in {containingObject.GetType().Name}", targetObject as UnityEngine.Object);
                return true;

            }
            // 条件フィールドの値を取得
            object fieldValue = getter(containingObject);

            foreach (int enumValue in attribute.enumValues)
            {
                
                bool result = fieldValue switch
                {
                    Enum e => attribute.useFlagMask 
                        ? ((int)Convert.ToInt64(e) & enumValue) != 0
                        : Convert.ToInt64(e) == enumValue,
                    _ => false
                };
                result = negate ? !result : result;
                if (result)
                    return true; // OR 条件なので、一つでも条件を満たせば true を返す
            }
            return false; // 条件を満たすものがなければ false を返す
        }
    }
}