namespace Syacapachi.Attribute
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |AttributeTargets.Enum |AttributeTargets.Interface)]
    public class GenerateEventAttribute : Attribute
    {
        public Type GenerateClass;
        public bool IsArray;
        public string Folder;
        public string ClassName;
        public bool RequireScriptableObject;

        public GenerateEventAttribute(
            Type generateClass,
            bool isArray = false,
            string folder = "Assets/Scripts/Generated",
            string className = null,
            bool requireScriptableObject = false)
        {
            Folder = folder;
            IsArray = isArray;
            ClassName = className;
            RequireScriptableObject = requireScriptableObject;
            GenerateClass = generateClass;
        }
    }
}