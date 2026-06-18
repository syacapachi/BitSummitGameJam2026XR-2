namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SingleFlagOnlyAttribute : PropertyAttribute
    {
        public readonly bool AllowNothing;

        public SingleFlagOnlyAttribute(bool allowNothing = true)
        {
            this.AllowNothing = allowNothing;
        }
    }
}