namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;

    public enum ConditionLogic
    {
        AND,
        OR,
        NOT,
        NAND,
        NOR,
        XOR,
        NXOR
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class EnableIfAttribute : PropertyAttribute
    {
        /// <summary>
        /// 名前の先頭に!をつけた場合否定になる
        /// </summary>
        public readonly string[] conditionFieldNames;
        public readonly bool hideWhenFalse;
        public readonly ConditionLogic logic;

        //ここに名前を入れる
        // 単一条件用コンストラクタ 
        // PropertyAttribute(bool applyToCollection)
        // true にすると Array/List の要素ではなく
        // コレクション本体に Drawer を適用する
        public EnableIfAttribute(string conditionFieldName, bool hideWhenFalse = false) : this(new[] { conditionFieldName }, ConditionLogic.AND, hideWhenFalse) { }

        // 複数条件用コンストラクタ
        public EnableIfAttribute(string[] conditionFieldNames, ConditionLogic logic = ConditionLogic.AND, bool hideWhenFalse = false) : base(true)
        {
            this.conditionFieldNames = conditionFieldNames;
            this.hideWhenFalse = hideWhenFalse;
            this.logic = logic;
        }
    }
}