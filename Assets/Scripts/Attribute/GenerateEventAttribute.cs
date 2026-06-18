namespace Syacapachi.Attribute
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |AttributeTargets.Enum |AttributeTargets.Interface)]
    public class GenerateEventAttribute : Attribute
    {
        public readonly Type GenerateClass;
        public readonly bool IsArray;
        public readonly string Folder;
        public readonly string ClassName;
        public readonly bool RequireScriptableObject;

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