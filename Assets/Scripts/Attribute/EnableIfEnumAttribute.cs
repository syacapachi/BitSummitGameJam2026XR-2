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
        /// <summary>
        /// 条件を判定するフィールド名
        /// </summary>
        public readonly string enumFiledName;
        /// <summary>
        /// 有効化対象のenumの値
        /// </summary>
        public readonly int[] enumValues;
        /// <summary>
        /// 偽の場合隠すかどうか
        /// </summary>
        public readonly bool hideWhenFalse;
        /// <summary>
        /// Maskで判定する
        /// </summary>
        public readonly bool useFlagMask;
        /// <summary>
        /// 先頭に!がある場合否定とする
        /// </summary>
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
