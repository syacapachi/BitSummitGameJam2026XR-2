#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(SingleFlagOnlyAttribute))]
    public class SingleFlagOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var attr = (SingleFlagOnlyAttribute)attribute;
            switch (property.propertyType)
            {
                // Enum型の場合、単一選択のUIを表示するためにカスタム描画を行います。
                case SerializedPropertyType.Enum:
                    //EditorGUI.BeginChangeCheck();// 変更の開始を通知
                    DrawEnum(position, property, label, attr);
                    break;

                default:
                    if (fieldInfo.FieldType == typeof(LayerMask))
                    {
                        //EditorGUI.BeginChangeCheck();
                        DrawLayerMask(position, property, label, attr);
                    }
                    else
                    {
                        EditorGUI.PropertyField(position, property, label);
                    }
                    break;
            }

            EditorGUI.EndProperty();
        }
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
            if (!IsSingleFlag(intValue,attr) && fieldInfo.FieldType.GetCustomAttribute<FlagsAttribute>() != null)
            {
                intValue = FixEnum(intValue, attr);
                GUI.color = Color.red;
                // ここで警告を表示することもできますが、頻繁に表示されると煩わしい可能性があるため、今回はコメントアウトしています。
                //EditorUtility.DisplayDialog("Invalid Selection", "Only one flag can be selected at a time.", "OK");
            }

            property.intValue = intValue;
        }

        // =========================
        // LayerMask
        // =========================
        void DrawLayerMask(Rect position, SerializedProperty property, GUIContent label, SingleFlagOnlyAttribute attr)
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

        bool IsSingleFlag(int value, SingleFlagOnlyAttribute attr)
        {
            return (attr.allowNothing || value != 0) && (value & (value - 1)) == 0;
        }

        int FixEnum(int value, SingleFlagOnlyAttribute attr)
        {
            if (value == 0 && !attr.allowNothing)
                return GetFirstEnumValue();

            return GetFirstBit(value);
        }

        int GetFirstBit(int value)
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

        int MaskToLayer(int mask)
        {
            if (mask == 0) return -1;

            for (int i = 0; i < 32; i++)
            {
                if (mask == (1 << i))
                    return i;
            }

            // 複数Layerが入ってた場合 → 最初の1つだけ返す
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return -1;
        }

        int LayerToMask(int layer)
        {
            if (layer < 0) return 0;
            return 1 << layer;
        }

        int GetFirstLayer()
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