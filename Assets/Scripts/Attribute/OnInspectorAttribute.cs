namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class OnInspectorButtonAttribute : PropertyAttribute
    {
        public string label;
        public bool showOnlyInPlayMode;

        /// <param name="label">ボタンのラベル（nullならメソッド名）</param>
        /// <param name="showOnlyInPlayMode">実行中のみ表示するか</param>
        public OnInspectorButtonAttribute(string label = null, bool showOnlyInPlayMode = false)
        {
            this.label = label;
            this.showOnlyInPlayMode = showOnlyInPlayMode;
        }
    }
}
