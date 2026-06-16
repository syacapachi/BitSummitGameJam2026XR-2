namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SceneAttribute : PropertyAttribute
    {
        public readonly bool BuildSettingsSceneOnly;
        public SceneAttribute(bool allowBuildSettingsSceneOnly = false)
        {
            this.BuildSettingsSceneOnly = allowBuildSettingsSceneOnly;
        }
    }
}