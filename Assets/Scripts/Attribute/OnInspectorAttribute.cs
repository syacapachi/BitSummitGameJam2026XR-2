namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class OnInspectorButtonAttribute : PropertyAttribute
    {
        /// <summary>
        /// ボタンのラベル。ない場合は関数名が表示されます。
        /// </summary>
        public string Label;
        /// <summary>
        /// 描画の優先度。初期値は0で、昇順に並びます。
        /// </summary>
        public int Order;
        /// <summary>
        /// Playモードのみ表示されるかどうか。
        /// </summary>
        public bool ShowOnlyInPlayMode;
        /// <summary>
        /// 値が変わったら実行するモードを有効にするか。
        /// </summary>
        public bool ValidateInvoke;
        /// <summary>
        /// 子クラスで表示しないか。
        /// </summary>
        public bool HideWhenChildClass;

        /// <param name="label">ボタンのラベル（nullならメソッド名）</param>
        /// <param name="showOnlyInPlayMode">実行中のみ表示するか</param>
        public OnInspectorButtonAttribute(string label = null,int order = 0, bool showOnlyInPlayMode = false, bool validateInvoke = false, bool hideWhenChildClass = false)
        {
            this.Label = label;
            this.Order = order;
            this.ShowOnlyInPlayMode = showOnlyInPlayMode;
            this.ValidateInvoke = validateInvoke;
            this.HideWhenChildClass = hideWhenChildClass;
        }
    }
}