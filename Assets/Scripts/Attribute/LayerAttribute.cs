namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LayerAttribute : PropertyAttribute
    {
        public bool useMask;
        public LayerAttribute(bool useMask = false)
        {
            this.useMask = useMask;
        }
    }
}