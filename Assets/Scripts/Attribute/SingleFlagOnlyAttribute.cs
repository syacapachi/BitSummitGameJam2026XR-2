namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SingleFlagOnlyAttribute : PropertyAttribute
    {
        public bool allowNothing { get; private set; }

        public SingleFlagOnlyAttribute(bool allowNothing = true)
        {
            this.allowNothing = allowNothing;
        }
    }
}