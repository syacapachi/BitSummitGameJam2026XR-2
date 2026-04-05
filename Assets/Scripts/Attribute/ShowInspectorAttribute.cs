using System;
using UnityEngine;

namespace Syacapachi.Attribute
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowInspectorAttribute : PropertyAttribute
    {
        public ShowInspectorAttribute() { }
    }
}

