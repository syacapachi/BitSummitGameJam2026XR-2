#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        private readonly struct EvaluationCacheKey : IEquatable<EvaluationCacheKey>
        {
            private readonly int targetId;
            private readonly string propertyPath;
            private readonly int attributeId;

            public EvaluationCacheKey(SerializedProperty property, EnableIfAttribute attribute)
            {
                UnityEngine.Object targetObject = property.serializedObject.targetObject;
                targetId = targetObject != null ? targetObject.GetInstanceID() : 0;
                propertyPath = property.propertyPath;
                attributeId = attribute.GetHashCode();
            }

            public readonly bool Equals(EvaluationCacheKey other)
            {
                return targetId == other.targetId
                    && attributeId == other.attributeId
                    && propertyPath == other.propertyPath;
            }

            public readonly override bool Equals(object obj)
            {
                return obj is EvaluationCacheKey other && Equals(other);
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(targetId, attributeId, propertyPath);
            }
        }

        private static readonly Dictionary<EvaluationCacheKey, bool> evaluationCache = new();
        private static int cacheFrame = -1;
        private static EventType cacheEventType = EventType.Ignore;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
            bool enabled = GetEnabled(property, enableIf);

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
            bool enabled = GetEnabled(property, enableIf);

            if (!enableIf.hideWhenFalse || enabled)
                return EditorGUI.GetPropertyHeight(property, label, true);
            //表示しない場合は隠すために高さ0を返す
            return 0f;
        }

        private static bool GetEnabled(SerializedProperty property, EnableIfAttribute enableIf)
        {
            PrepareCacheForCurrentDraw();
            EvaluationCacheKey key = new(property, enableIf);
            if (!evaluationCache.TryGetValue(key, out bool enabled))
            {
                enabled = EnableIfEvaluator.EvaluateConditionsRecursive(property, enableIf);
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
