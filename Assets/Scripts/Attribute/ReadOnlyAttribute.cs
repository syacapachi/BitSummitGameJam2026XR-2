namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public readonly bool ShowOnlyPlayMode;
        public ReadOnlyAttribute(bool showOnlyPlayMode = false)
        {
            this.ShowOnlyPlayMode = showOnlyPlayMode;
        }
    }
}