#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SingleFlagOnlyAttribute))]
    public class SingleFlagOnlyDrawer : PropertyDrawer
    {
        // ========================================
        // Flags enum キャッシュ
        // ========================================

        static readonly Dictionary<Type, bool> isFlagsCache = new();

        private static readonly GUIContent warningLabel = new GUIContent($"Use with Enum or LayerMask fields only.");
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (SingleFlagOnlyAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);

            switch (property.propertyType)
            {
                // Enum型の場合、単一選択のUIを表示するためにカスタム描画を行います。
                case SerializedPropertyType.Enum:
                    //EditorGUI.BeginChangeCheck();// 変更の開始を通知
                    DrawEnumWithArrayOrList(position, fieldInfo.FieldType, property, label, attr);
                    break;
                case SerializedPropertyType.LayerMask:
                    DrawLayerMask(position, property, label, attr);
                    break;
                default:
                    //EditorGUI.PropertyField(position, property, label,true);
                    EditorGUI.LabelField(position, label, warningLabel);
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
        static void DrawEnum(in Rect position, Type type, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
        {
            int rawValue = property.intValue;
            Enum enumValue = (Enum)Enum.ToObject(type, rawValue);

            // EnumPopupで表示(単一表示)
            Enum newValue = EditorGUI.EnumPopup(position, label, enumValue);
            // EnumFlagsFieldは複数選択UIになるため、単一選択のEnumPopupを使用しています。
            //Enum newValue = EditorGUI.EnumFlagsField(position, label, enumValue);
            int intValue = Convert.ToInt32(newValue);

            // 単一化
            if (!IsSingleFlag(intValue, attr) && IsFlagsEnum(type))
            {
                intValue = FixEnum(type, intValue, attr);
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
        private static void DrawEnumWithArrayOrList(in Rect position, Type type, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
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
            
            Type enumType = GetEnumType(type);
            // enum実値取得
            int intValue = Convert.ToInt32(
                //index をenumに変換
                Enum.Parse(
                    enumType,
                    property.enumNames[newIndex]));
         

            // Flags enum の場合のみ単一化チェック
            if (!IsSingleFlag(intValue, attr) && IsFlagsEnum(enumType))
            {
                intValue = FixEnum(enumType, intValue, attr);

                GUI.color = Color.red;

                // Fix後の値に対応するindexへ戻す
                string fixedName = Enum.GetName(enumType, intValue);

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

            property.enumValueIndex = newIndex;
        }
        static bool IsFlagsEnum(Type type)
        {
            if (isFlagsCache.TryGetValue(type, out bool result))
                return result;

            result =
                type.IsEnum &&
                type.IsDefined(typeof(FlagsAttribute), false);

            isFlagsCache[type] = result;

            return result;
        }

        private static Type GetEnumType(Type type)
        {
            if (type.IsEnum) return type;

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
        static void DrawLayerMask(in Rect position, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
        {
            // LayerMaskはintで保持されているため、MaskとLayerの変換が必要
            int mask = property.intValue;

            int currentLayer = MaskToLayer(mask);
            // MaskField（複数選択UI）
            //EditorGUI.MaskField(position, label, currentLayer, InternalEditorUtility.layers);

            // LayerFild（単一選択UI）
            int selectedLayer = EditorGUI.LayerField(position, label, currentLayer);

            // Nothing許可しない場合
            if (!attr.AllowNothing && selectedLayer == -1)
            {
                selectedLayer = 0; // Defaultにフォールバック
            }

            property.intValue = LayerToMask(selectedLayer);
        }

        // =========================
        // 共通処理
        // =========================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsSingleFlag(int value, SingleFlagOnlyAttribute attr)
        {
            return (attr.AllowNothing || value != 0) && (value & (value - 1)) == 0;
        }

        static int FixEnum(Type enumType, int value, SingleFlagOnlyAttribute attr)
        {
            if (value == 0 && !attr.AllowNothing)
                return GetFirstEnumValue(enumType);

            return GetFirstBit(value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetFirstBit(int value)
        {
            if (value == 0) return 0;
            return value & -value; // 最下位ビットだけ残す
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetFirstEnumValue(Type enumType)
        {
            foreach (int v in Enum.GetValues(enumType))
            {
                if (v != 0) return v;
            }
            return 0;
        }
        // =========================
        // Utility
        // =========================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            }
            ;
            return 0;//Defaultにする
        }
        /// <summary>
        /// Enumをulongに読み替える(object)(参照)->Enum(値)だと結局boxingが起きるので意味ない。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ulong EnumToUInt64<T>(T value) where T : unmanaged, Enum
        {
            return Unsafe.SizeOf<T>() switch
            {
                1 => Unsafe.As<T, byte>(ref value),
                2 => Unsafe.As<T, ushort>(ref value),
                4 => Unsafe.As<T, uint>(ref value),
                8 => Unsafe.As<T, ulong>(ref value),
                _ => throw new InvalidOperationException()
            };
        }
    }
}
#endif