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
        XOR
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class EnableIfAttribute : PropertyAttribute
    {
        /// <summary>
        /// 名前の先頭に!をつけた場合否定になる
        /// </summary>
        public string[] conditionFieldNames;
        public bool hideWhenFalse;
        public ConditionLogic logic;

        //ここに名前を入れる
        public EnableIfAttribute(string conditionFieldName, bool hideWhenFalse = false)
        {
            this.conditionFieldNames = new[] { conditionFieldName };
            this.hideWhenFalse = hideWhenFalse;
            this.logic = ConditionLogic.AND;
        }

        // 複数条件用コンストラクタ
        public EnableIfAttribute(string[] conditionFieldNames, ConditionLogic logic = ConditionLogic.AND, bool hideWhenFalse = false)
        {
            this.conditionFieldNames = conditionFieldNames;
            this.hideWhenFalse = hideWhenFalse;
            this.logic = logic;
        }
    }
}