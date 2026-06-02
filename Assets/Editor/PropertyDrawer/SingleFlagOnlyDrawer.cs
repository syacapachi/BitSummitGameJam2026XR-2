#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SingleFlagOnlyAttribute))]
    public class SingleFlagOnlyDrawer : PropertyDrawer
    {
        // ========================================
        // Flags enum キャッシュ
        // ========================================

        static readonly HashSet<Type> flagsEnumTypes = new();

        // 静的コンストラクタでAppDomain全体からFlags属性が付与されたenumを収集してキャッシュします。
        //TypeCache.GetTypesWithAttribute<FlagsAttribute>()を使用する方法もありますが、UnityのTypeCacheはCLR標準属性で、Unityの TypeCache が高速化対象として完全に最適化していないケースもあるため、独自に収集しています。
        static SingleFlagOnlyDrawer()
        {
            // AppDomain全体からenumを収集
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (!type.IsEnum)
                        continue;

                    if (type.IsDefined(typeof(FlagsAttribute), false))
                    {
                        flagsEnumTypes.Add(type);
                    }
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var attr = (SingleFlagOnlyAttribute)attribute;
            switch (property.propertyType)
            {
                // Enum型の場合、単一選択のUIを表示するためにカスタム描画を行います。
                case SerializedPropertyType.Enum:
                    //EditorGUI.BeginChangeCheck();// 変更の開始を通知
                    DrawEnumWithArrayOrList(position, property, label, attr);
                    break;
                case SerializedPropertyType.LayerMask:
                    DrawLayerMask(position, property, label, attr);
                    break;
                default:
                    //EditorGUI.PropertyField(position, property, label,true);
                    EditorGUI.LabelField(position, label.text, $"{nameof(SingleFlagOnlyAttribute)} with Enum or LayerMask fields only.");
                    break;
            }

            EditorGUI.EndProperty();
        }
        //public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        //{
        //    return property.propertyType switch
        //    {
        //        // Enum型の場合、単一選択のUIを表示するためにカスタム描画を行います。
        //        SerializedPropertyType.Enum => base.GetPropertyHeight(property, label),
        //        SerializedPropertyType.LayerMask => base.GetPropertyHeight(property, label),
        //        _ => base.GetPropertyHeight(property, label) * 3,
        //    };
        //}
        // =========================
        // Enum
        // =========================
        void DrawEnum(Rect position, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
        {
            int rawValue = property.intValue;
            Enum enumValue = (Enum)Enum.ToObject(fieldInfo.FieldType, rawValue);

            // EnumPopupで表示(単一表示)
            Enum newValue = EditorGUI.EnumPopup(position, label, enumValue);
            // EnumFlagsFieldは複数選択UIになるため、単一選択のEnumPopupを使用しています。
            //Enum newValue = EditorGUI.EnumFlagsField(position, label, enumValue);
            int intValue = Convert.ToInt32(newValue);

            // 単一化
            if (!IsSingleFlag(intValue,attr) && flagsEnumTypes.Contains(fieldInfo.FieldType))
            {
                intValue = FixEnum(intValue, attr);
                GUI.color = Color.red;
                // ここで警告を表示することもできますが、頻繁に表示されると煩わしい可能性があるため、今回はコメントアウトしています。
                //EditorUtility.DisplayDialog("Invalid Selection", "Only one flag can be selected at a time.", "OK");
            }

            property.intValue = intValue;
        }
        /// <summary>
        /// Array,ListなどのEnum型に対して、単一選択のUIを表示するためのカスタム描画を行います。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        /// <param name="attr"></param>
        private void DrawEnumWithArrayOrList(Rect position, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
        {
            // enum情報取得
            string[] displayNames = property.enumDisplayNames;
            int currentIndex = property.enumValueIndex;

            // 念のため範囲補正
            if (currentIndex < 0 || currentIndex >= displayNames.Length)
            {
                currentIndex = 0;
            }

            // Popup描画(EnumPopupも内部でEnumを変換してこれを使っている。)
            int newIndex = EditorGUI.Popup(
                position,
                label.text,
                currentIndex,
                displayNames);

            // 変更なし
            if (newIndex == currentIndex)
            {
                return;
            }
            Type enumType = GetEnumType();
            // enum実値取得
            int intValue = Convert.ToInt32(
                Enum.Parse(
                    enumType,
                    property.enumNames[newIndex]));

            // Flags enum の場合のみ単一化チェック
            if (flagsEnumTypes.Contains(enumType))
            {
                if (!IsSingleFlag(intValue, attr))
                {
                    intValue = FixEnum(intValue, attr);

                    GUI.color = Color.red;

                    // Fix後の値に対応するindexへ戻す
                    string fixedName = Enum.GetName(GetEnumType(), intValue);

                    if (!string.IsNullOrEmpty(fixedName))
                    {
                        int fixedIndex = Array.IndexOf(
                            property.enumNames,
                            fixedName);

                        if (fixedIndex >= 0)
                        {
                            newIndex = fixedIndex;
                        }
                    }
                }
            }

            property.enumValueIndex = newIndex;
        }
        private Type GetEnumType()
        {
            Type type = fieldInfo.FieldType;

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                Type[] args = type.GetGenericArguments();

                if (args.Length > 0)
                {
                    return args[0];
                }
            }

            return type;
        }
        // =========================
        // LayerMask
        // =========================
        static void DrawLayerMask(Rect position, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
        {
            // LayerMaskはintで保持されているため、MaskとLayerの変換が必要
            int mask = property.intValue;

            int currentLayer = MaskToLayer(mask);
            // MaskField（複数選択UI）
            //EditorGUI.MaskField(position, label, currentLayer, InternalEditorUtility.layers);

            // LayerFild（単一選択UI）
            int selectedLayer = EditorGUI.LayerField(position,label, currentLayer);
            
            // Nothing許可しない場合
            if (!attr.allowNothing && selectedLayer == -1)
            {
                selectedLayer = 0; // Defaultにフォールバック
            }

            property.intValue = LayerToMask(selectedLayer);   
        }

        // =========================
        // 共通処理
        // =========================

        static bool IsSingleFlag(int value, SingleFlagOnlyAttribute attr)
        {
            return (attr.allowNothing || value != 0) && (value & (value - 1)) == 0;
        }

        int FixEnum(int value, SingleFlagOnlyAttribute attr)
        {
            if (value == 0 && !attr.allowNothing)
                return GetFirstEnumValue();

            return GetFirstBit(value);
        }

        static int GetFirstBit(int value)
        {
            if (value == 0) return 0;
            return value & -value; // 最下位ビットだけ残す
        }

        int GetFirstEnumValue()
        {
            foreach (int v in Enum.GetValues(fieldInfo.FieldType))
            {
                if (v != 0) return v;
            }
            return 0;
        }
        // =========================
        // Utility
        // =========================

        static int MaskToLayer(int mask)
        {
            if (mask <= 0) return 0;

            // 複数Layerが入ってた場合 → 最初の1つだけ返す
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }

        static int LayerToMask(int layer)
        {
            if (layer < 0) return 0;
            return 1 << layer;
        }

        static int GetFirstLayer()
        {
            // レイヤー名配列取得
            string[] layers = InternalEditorUtility.layers;
            foreach (string layerName in layers) 
            {
                int value = LayerMask.NameToLayer(layerName);
                if (value != -1)
                {
                    return 1 << value;
                }
            };
            return 0;//Defaultにする
        }
    }
}
#endif