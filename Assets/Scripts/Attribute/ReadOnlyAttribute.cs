namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public bool showOnlyPlayMode;
        public ReadOnlyAttribute(bool showOnlyPlayMode = false)
        {
            this.showOnlyPlayMode = showOnlyPlayMode;
        }
    }
}