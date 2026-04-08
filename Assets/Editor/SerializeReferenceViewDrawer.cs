#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using System.Reflection;
    using System;
    using UnityEditor;

    using UnityEngine;
    using System.Linq;
    using Syacapachi.Attribute;

    /// <summary>
    /// [SerializeReferenceView] 用 PropertyDrawer
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializeReferenceViewAttribute))]
    public class SerializeReferenceViewDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (SerializeReferenceViewAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            if (property.managedReferenceValue == null)
            {
                // タイプ選択ボタン
                if (GUI.Button(position, "＋ 型を選択"))
                {
                    ShowTypeMenu(property, attr.BaseType);
                }
            }
            else
            {
                // クラス名をタイトルに表示
                var type = property.managedReferenceValue.GetType();
                Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(headerRect, $"▶ {type.Name}", EditorStyles.boldLabel);

                // 削除ボタン 属性をnullにして更新
                Rect btnRect = new Rect(position.x + position.width - 60, position.y, 60, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btnRect, "削除"))
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
            if (property.managedReferenceValue == null)
                return EditorGUIUtility.singleLineHeight * 1.2f;

            return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.singleLineHeight;
        }

        private void ShowTypeMenu(SerializedProperty property, Type baseType)
        {
            GenericMenu menu = new GenericMenu();

            Type fieldType = fieldInfo.FieldType;
            Type targetBase = baseType ?? fieldType;

            // ジェネリックなども含め全型,基底クラスを継承するクラスを探索
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    targetBase.IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface);

            foreach (var type in types)
            {
                string menuName = type.FullName.Replace('.', '/');
                menu.AddItem(new GUIContent(menuName), false, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}
#endif