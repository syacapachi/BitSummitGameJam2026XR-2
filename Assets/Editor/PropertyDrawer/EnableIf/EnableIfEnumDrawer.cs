#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(EnableIfEnumAttribute))]
    public class EnableIfEnumDrawer : PropertyDrawer
    {
        private readonly struct EvaluationCacheKey : IEquatable<EvaluationCacheKey>
        {
            readonly int targetId;
            readonly int propertyHash;
            readonly int conditionHash;

            public EvaluationCacheKey(SerializedProperty property, EnableIfEnumAttribute attribute)
            {
                targetId =
                    property.serializedObject.targetObject
                    .GetInstanceID();

                propertyHash =
                    property.GetHashCode();

                conditionHash =
                    HashCode.Combine(
                        attribute.GetType(),
                        attribute.enumFiledName);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Equals(EvaluationCacheKey other)
            {
                return GetHashCode() == other.GetHashCode();
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]

            public readonly override bool Equals(object obj)
            {
                return obj is EvaluationCacheKey other && Equals(other);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly override int GetHashCode()
            {
                return HashCode.Combine(targetId, conditionHash, propertyHash);
            }
        }

        private static readonly Dictionary<EvaluationCacheKey, bool> evaluationCache = new();
        private static int cacheFrame = -1;
        private static EventType cacheEventType = EventType.Ignore;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnableIfEnumAttribute condition = (EnableIfEnumAttribute)attribute;
            //string conditionPath = property.propertyPath.Replace(property.name, condition.enumFiledName);
            //// Enumを参照するプロパティを検索
            //SerializedProperty enumProp = property.serializedObject.FindProperty(conditionPath);
            //bool isValidEnum = enumProp != null && IsEnumValueValid(enumProp, condition.enumValues);
            bool isValidEnum = GetEnabled(property, condition);

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
            bool isValidEnum = GetEnabled(property, condition);
            if (!condition.hideWhenFalse || isValidEnum)
                return EditorGUI.GetPropertyHeight(property, label, true);
            //表示しない場合は隠すために高さ0を返す
            return 0f;
        }

        private static bool GetEnabled(SerializedProperty property, EnableIfEnumAttribute condition)
        {
            PrepareCacheForCurrentDraw();
            EvaluationCacheKey key = new(property, condition);
            if (!evaluationCache.TryGetValue(key, out bool enabled))
            {
                enabled = EnableIfEvaluator.EvaluateEnumConditionRecursive(property, condition);
                evaluationCache[key] = enabled;
            }
            return enabled;
        }

        private static void PrepareCacheForCurrentDraw()
        {
            Event currentEvent = Event.current;
            EventType eventType = currentEvent != null ? currentEvent.type : EventType.Ignore;
            int frame = Time.frameCount;

            if (cacheFrame == frame && cacheEventType == eventType)
                return;

            evaluationCache.Clear();
            cacheFrame = frame;
            cacheEventType = eventType;
        }
    }
}
#endif
