namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LayerAttribute : PropertyAttribute
    {
        public readonly bool UseMask;
        public LayerAttribute(bool useMask = false)
        {
            this.UseMask = useMask;
        }
    }
}