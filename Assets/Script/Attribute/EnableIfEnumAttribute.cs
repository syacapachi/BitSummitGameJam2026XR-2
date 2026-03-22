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
        public string enumFiledName; //条件を判定するフィールド名
        public int[] enumValues;   //有効か対象のenumの値
        public bool hideWhenFalse;

        public EnableIfEnumAttribute(string enumFiledName, bool hideWhenFalse = false, params object[] enumValues)
        {
            this.enumFiledName = enumFiledName;
            this.enumValues = new int[enumValues.Length];
            for (int i = 0; i < enumValues.Length; i++)
            {
                this.enumValues[i] = (int)enumValues[i];
            }

            this.hideWhenFalse = hideWhenFalse;
        }
        //public EnableIfEnumAttribute(string enumFiledName, params object[] enumValues)
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