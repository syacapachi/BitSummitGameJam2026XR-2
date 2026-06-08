#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TextCore.Text;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ShowInspectorAttribute))]
    public class ShowInspectorDrawer : PropertyDrawer
    {
        // メソッドと引数のキャッシュ (パフォーマンス向上のため)
        private static readonly Dictionary<FieldInfo, object> fieldParameter = new();
        // Foldoutの状態のキャッシュ (複数インスペクターでの状態管理のため)
        private static readonly Dictionary<object, bool> foldouts = new();
        // ScriptableObjectのFoldout状態のキャッシュ (複数インスペクターでの状態管理のため)
        private static readonly Dictionary<UnityEngine.Object, bool> foldoutStates = new();
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowInspectorAttribute)attribute;
            Type targetType = fieldInfo.FieldType;
            SerializedPropertyType type = property.propertyType;

            //プロパティはラッパークラスなので、実際のオブジェクトを取得する必要がある
            //しかし、値を変更する場合はラッパークラス越しに変更する必要があるため、プロパティから直接値を取得して描画するのではなく、リフレクションでフィールドの値を取得して描画する。
            object target = property.serializedObject.targetObject;


            EditorGUI.BeginProperty(position, label, property);
            //UnityEngine.Object以外
            if (type == SerializedPropertyType.ManagedReference)
            {
                DrawField(position, targetType, label.text, fieldInfo.GetValue(target));
            }
            //UnityEngine.Object
            else if (type == SerializedPropertyType.ObjectReference)
            {
                DrawField(position, targetType, label.text, fieldInfo.GetValue(target));
            }
            //Scene内の参照
            else if(type == SerializedPropertyType.ExposedReference)
            {
                DrawField(position, targetType, label.text, fieldInfo.GetValue(target));
            }
            else
            {
                DrawField(position, targetType, label.text, fieldInfo.GetValue(target));
            }
            EditorGUI.EndProperty();

        }
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return base.CreatePropertyGUI(property);
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        //ネストにも対応させる
        private object DrawField(Rect rect,Type t, string name, object currentValue)
        {
            name = ObjectNames.NicifyVariableName(name);
            if (t == typeof(int))
                return EditorGUI.IntField(rect, name, currentValue != null ? (int)currentValue : 0);
            if (t == typeof(byte))
                return EditorGUI.IntField(rect, name, currentValue != null ? (int)(byte)currentValue : 0);
            if (t == typeof(uint))
                return EditorGUI.IntField(rect, name, currentValue != null ? (int)(uint)currentValue : 0);
            if (t == typeof(short))
                return EditorGUI.IntField(rect, name, currentValue != null ? (int)(short)currentValue : 0);
            if (t == typeof(float))
                return EditorGUI.FloatField(rect, name, currentValue != null ? (float)currentValue : 0f);
            if (t == typeof(double))
                return EditorGUI.DoubleField(rect, name, currentValue != null ? (double)currentValue : 0);
            if (t == typeof(long))
                return EditorGUI.LongField(rect, name, currentValue != null ? (long)currentValue : 0);
            if (t == typeof(string))
                return EditorGUI.TextField(rect, name, currentValue as string ?? "");
            if (t == typeof(bool))
                return EditorGUI.Toggle(rect, name, currentValue != null && (bool)currentValue);
            if (t == typeof(Vector2))
                return EditorGUI.Vector2Field(rect, name, currentValue != null ? (Vector2)currentValue : Vector2.zero);
            if (t == typeof(Vector3))
                return EditorGUI.Vector3Field(rect, name, currentValue != null ? (Vector3)currentValue : Vector3.zero);
            if (t == typeof(Vector4))
                return EditorGUI.Vector4Field(rect, name, currentValue != null ? (Vector4)currentValue : Vector4.zero);
            if (t == typeof(Vector2Int))
                return EditorGUI.Vector2IntField(rect, name, currentValue != null ? (Vector2Int)currentValue : Vector2Int.zero);
            if (t == typeof(Vector3Int))
                return EditorGUI.Vector3IntField(rect, name, currentValue != null ? (Vector3Int)currentValue : Vector3Int.zero);
            if (t == typeof(Color))
                return EditorGUI.ColorField(rect, name, currentValue != null ? (Color)currentValue : Color.white);
            if (t == typeof(Rect))
                return EditorGUI.RectField(rect, name, currentValue != null ? (Rect)currentValue : new Rect());
            if (t == typeof(Bounds))
                return EditorGUI.BoundsField(rect, name, currentValue != null ? (Bounds)currentValue : new Bounds());
            if (t == typeof(AnimationCurve))
                return EditorGUI.CurveField(rect, name, currentValue as AnimationCurve ?? new AnimationCurve());
            if (t == typeof(Gradient))
                return EditorGUI.GradientField(rect, name, currentValue as Gradient ?? new Gradient());
            if(t == typeof(LayerMask))
                return EditorGUI.LayerField(rect, name, currentValue != null ? (LayerMask)currentValue : new LayerMask());
            // Enum
            if (t.IsEnum)
            {
                currentValue ??= Enum.GetValues(t).GetValue(0);
                return EditorGUI.EnumPopup(rect, name, (Enum)currentValue);
            }

            // UnityEngine.Object
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                var obj = currentValue as UnityEngine.Object;

                obj = EditorGUI.ObjectField(rect, name, obj, t, true);

                if (obj is ScriptableObject so)
                    DrawScriptableObjectInline(so);

                return obj;
            }
            // 配列
            if (t.IsArray)
            {
                Type elementType = t.GetElementType();
                IList list = currentValue as IList;
                return DrawList(name, rect, elementType, list);
            }
            // List
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = t.GetGenericArguments()[0];
                IList list = currentValue as IList;
                return DrawList(name, rect, elementType, list);
            }
            // 辞書
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return DrawDictionary(name, rect, t, currentValue);
            }
            // ScriptableObjectをインラインで描画
            return DrawObject(name, rect, t, currentValue);
        }
        IList DrawList(string name, Rect rect, Type elementType, IList list)
        {
            // nullの場合は新しいリストを作成
            list ??= (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            // Foldoutの状態をリスト自体で管理することで、同じリストを複数のインスペクターで描画している場合でも、展開状態を共有できる。
            bool fold = GetFoldout(list);

            // Foldoutを描画して状態を更新
            fold = EditorGUI.Foldout(rect, fold, $"{name} [{list.Count}]");
            SetFoldout(list, fold);

            if (!fold)
                return list;

            //展開されている場合は要素を描画
            EditorGUI.indentLevel++;

            int size = EditorGUI.IntField(rect, "Size", list.Count);

            while (list.Count < size)
                list.Add(GetDefault(elementType));

            while (list.Count > size)
                list.RemoveAt(list.Count - 1);

            for (int i = 0; i < list.Count; i++)
            {
                //要素を描画して更新
                list[i] = DrawField(rect, elementType, $"Element {i}", list[i]);
            }

            EditorGUI.indentLevel--;

            return list;
        }

        object DrawDictionary(string name, Rect rect, Type dictType, object dictObj)
        {
            var args = dictType.GetGenericArguments();

            Type keyType = args[0];
            Type valueType = args[1];

            IDictionary dict = dictObj as IDictionary;

            dict ??= (IDictionary)Activator.CreateInstance(dictType);

            bool fold = GetFoldout(dict);

            fold = EditorGUI.Foldout(rect, fold, $"{name} [{dict.Count}]");
            SetFoldout(dict, fold);

            if (!fold)
                return dict;

            EditorGUI.indentLevel++;

            //辞書のキーは変更できるようにするため、キーのリストを作成してループする。そうしないと、ループ中にキーが変更されたときに、辞書の状態が不安定になる。
            List<object> keys = new(dict.Keys.Count + 10);

            foreach (var k in dict.Keys)
                keys.Add(k);

            foreach (var key in keys)
            {
                EditorGUI.BeginChangeCheck();

                object newKey = DrawField(rect, keyType, "Key", key);
                object newValue = DrawField(rect, valueType, "Value", dict[key]);

                //キーが変更された場合は、古いキーを削除して新しいキーで追加。そうでない場合は値だけ更新。
                if (!Equals(newKey, key))
                {
                    dict.Remove(key);
                    dict[newKey] = newValue;
                }
                else
                {
                    dict[key] = newValue;
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    dict.Remove(key);
                    break;
                }

                EditorGUI.EndChangeCheck();
            }

            if (GUILayout.Button("Add"))
            {
                dict[GetDefault(keyType)] = GetDefault(valueType);
            }

            EditorGUI.indentLevel--;

            return dict;
        }
        object DrawObject(string name, Rect rect, Type type, object value)
        {
            value ??= Activator.CreateInstance(type);

            bool fold = GetFoldout(value);

            fold = EditorGUILayout.Foldout(fold, name,true);

            SetFoldout(value, fold);

            if (!fold)
                return value;

            EditorGUI.indentLevel++;

            //フィールドを列挙して描画
            var fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            foreach (var f in fields)
            {
                var fieldValue = f.GetValue(value);

                var newValue = DrawField(rect, f.FieldType, f.Name, fieldValue);

                if (!Equals(fieldValue, newValue))
                    f.SetValue(value, newValue);
            }

            EditorGUI.indentLevel--;

            return value;
        }
        void DrawScriptableObjectInline(ScriptableObject so)
        {
            //if (so == null)
            //    return;

            //if (!editorCache.TryGetValue(so, out var editor))
            //{
            //    editor = CreateEditor(so);
            //    editorCache[so] = editor;
            //}

            //EditorGUI.BeginChangeCheck();

            //editor.OnInspectorGUI();

            //EditorGUI.EndChangeCheck();
        }
        object GetDefault(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }

        bool GetFoldout(object key)
        {
            if (!foldouts.TryGetValue(key, out bool value))
            {
                value = false;
                foldouts[key] = value;
            }

            return value;
        }

        void SetFoldout(object key, bool value)
        {
            foldouts[key] = value;
        }
    }
}
#endif