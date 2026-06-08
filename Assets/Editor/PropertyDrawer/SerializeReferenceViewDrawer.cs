#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// [SerializeReferenceView] 用 PropertyDrawer
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializeReferenceViewAttribute))]
    public class SerializeReferenceViewDrawer : PropertyDrawer
    {
        readonly struct TypeNameCache
        {
            public readonly GUIContent TypeName;
            public readonly GUIContent GUIContent;

            public TypeNameCache(Type type)
            {
                TypeName = new GUIContent($"▶ {type.Name}");
                GUIContent = new GUIContent(type.FullName.Replace('.', '/'));
            }
        }
        private static readonly GUIContent selectButtonLabel = new GUIContent("＋ 型を選択");
        private static readonly GUIContent deleteButtonLabel = new GUIContent("削除");
        //TypeとGUIContentのキャッシュ
        private static readonly Dictionary<Type, TypeNameCache> typeNameCache = new();
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (SerializeReferenceViewAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);
            if (property.managedReferenceValue == null)
            {
                // タイプ選択ボタン
                if (GUI.Button(position, selectButtonLabel))
                {
                    //Debug.Log($"Search {fieldInfo.FieldType}");
                    ShowTypeMenu(property, attr, fieldInfo.FieldType);
                }
            }
            else
            {
                // クラス名をタイトルに表示
                var type = property.managedReferenceValue.GetType();
                Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                var cache = GetOrCreateCache(type);
                EditorGUI.LabelField(headerRect, cache.TypeName, EditorStyles.boldLabel);

                // 削除ボタン 属性をnullにして更新
                Rect btnRect = new Rect(position.x + position.width - 60, position.y, 60, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btnRect, deleteButtonLabel))
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                    return;
                }

                // 子プロパティ描画
                EditorGUI.indentLevel++;//インデックスを下へ
                var body = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, position.height);
                //属性に含まれるシリアル化された情報を描画
                EditorGUI.PropertyField(body, property, true);
                EditorGUI.indentLevel--;//インデックスを戻す
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //何もないならスペースを1.2fに
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUIUtility.singleLineHeight * 1.2f;

            return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.singleLineHeight;
        }
        /// <summary>
        /// 派生クラスを決定するGenericMenuを作成する。
        /// </summary>
        /// <param name="property"> 描画するSerializedProperty </param>
        /// <param name="attr">FieldのSerializeReferenceViewAttribute </param>
        /// <param name="fieldType">　filedInfo.FieldType </param>
        private static void ShowTypeMenu(SerializedProperty property, SerializeReferenceViewAttribute attr, Type fieldType)
        {
            GenericMenu menu = new GenericMenu();
            //Array
            if (fieldType.IsArray)
            {
                fieldType = fieldType.GetElementType();
            }
            //List
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                fieldType = fieldType.GetGenericArguments()[0];
            }
            Type targetBase = attr.BaseType ?? fieldType;

            // ジェネリックなども含め全型,基底クラスを継承するクラスを探索
            //var types = AppDomain.CurrentDomain.GetAssemblies()
            //    .SelectMany(a => a.GetTypes())
            //    .Where(t =>
            //        targetBase.IsAssignableFrom(t) &&
            //        !t.IsAbstract &&
            //        !t.IsInterface);

            //Unity内部のキャッシュを活用することで高速化
            var types = TypeCache.GetTypesDerivedFrom(targetBase);

            //自身
            if (!targetBase.IsAbstract && !targetBase.IsInterface)
            {
                var cache = GetOrCreateCache(targetBase);
                menu.AddItem(cache.GUIContent, false, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(targetBase);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            foreach (var type in types)
            {
                var cache = GetOrCreateCache(type);
                menu.AddItem(cache.GUIContent, false, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
        /// <summary>
        /// キャッシュ取得
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static TypeNameCache GetOrCreateCache(Type type)
        {
            if (!typeNameCache.TryGetValue(type, out var cache))
            {
                cache = new TypeNameCache(type);
                typeNameCache[type] = cache;
            }
            return cache;
        }
    }
}
#endif