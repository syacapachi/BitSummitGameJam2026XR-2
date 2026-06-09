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
        /// <summary>
        /// 名前の先頭に!があったやつ
        /// </summary>
        public readonly bool[] conditionNegates;
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
            this.conditionFieldNames = new string[conditionFieldNames.Length];
            conditionNegates = new bool[conditionFieldNames.Length];
            for (int i = 0; i < conditionFieldNames.Length; i++)
            {
                string fieldName = conditionFieldNames[i];
                bool isNegate = !string.IsNullOrEmpty(fieldName) && fieldName[0] == '!';
                conditionNegates[i] = isNegate;
                this.conditionFieldNames[i] = isNegate ? fieldName.Substring(1) : fieldName;
            }
            this.hideWhenFalse = hideWhenFalse;
            this.logic = logic;
        }
    }
}
