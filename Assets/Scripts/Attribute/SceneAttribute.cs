namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SceneAttribute : PropertyAttribute
    {
        public bool buildSettingsSceneOnly;
        public SceneAttribute(bool allowBuildSettingsSceneOnly = false)
        {
            this.buildSettingsSceneOnly = allowBuildSettingsSceneOnly;
        }
    }
}