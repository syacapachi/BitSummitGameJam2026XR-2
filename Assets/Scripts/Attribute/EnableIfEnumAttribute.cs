namespace Syacapachi.Attribute
{


    using System;
    using UnityEngine;

    /// <summary>
    /// 対象のEnum変数の値によって、編集可能・不可能を切り替えます。
    /// <param name="enumFiledName"> enumFiledName:比較対象のEnumタイプの変数</param>
    /// <param name="hideWhenFalse"> hideWhenFalse:隠す設定の時</param>
    /// <param name="enumValues"> 対象のEnum値配列,複数の場合は、いずれか1つ選ばれた場合に編集可能になります</param>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnableIfEnumAttribute : PropertyAttribute
    {
        public readonly string enumFiledName; //条件を判定するフィールド名
        public readonly int[] enumValues;   //有効か対象のenumの値
        public readonly bool hideWhenFalse;
        public readonly bool useFlagMask;
        public readonly bool negate;

        // PropertyAttribute(bool applyToCollection)
        // true にすると Array/List の要素ではなく
        // コレクション本体に Drawer を適用する
        public EnableIfEnumAttribute(string enumFiledName, bool hideWhenFalse = false, bool useFlagMask = false, params int[] enumValues) : base(true)
        {
            negate = !string.IsNullOrEmpty(enumFiledName) && enumFiledName[0] == '!';
            this.enumFiledName = negate ? enumFiledName.Substring(1) : enumFiledName;
            //enumValuesをint型の配列として受け取る
            this.enumValues = new int[enumValues.Length];
            for (int i = 0; i < enumValues.Length; i++)
            {
                this.enumValues[i] = enumValues[i];
            }

            this.hideWhenFalse = hideWhenFalse;
            this.useFlagMask = useFlagMask;
        }
        //別コンストラクタ呼び出し。
        public EnableIfEnumAttribute(string enumFiledName,bool hideWhenFalse, params int[] enumValues) : this(enumFiledName, hideWhenFalse, false, enumValues) { }
        public EnableIfEnumAttribute(string enumFiledName, params int[] enumValues) : this(enumFiledName, false, false, enumValues) { }

        //public EnableIfEnumAttribute(string enumFiledName, params object[] enumValues): base(true)
        //{
        //    this.enumFiledName = enumFiledName;
        //    this.enumValues = new int[enumValues.Length];
        //    for (int i = 0; i < enumValues.Length; i++)
        //    {
        //        this.enumValues[i] = (int)enumValues[i];
        //    }
        //    this.hideWhenFalse = false;
        //}
    }
}
