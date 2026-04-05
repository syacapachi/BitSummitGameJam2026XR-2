#if UNITY_EDITOR
namespace Syacapachi.util
{
    using Syacapachi.Attribute;
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;
    //呼ばれすぎる
    //[InitializeOnLoad]
    public static class AutoEventGenerator
    {
        static AutoEventGenerator()
        {
            GenerateAll();
        }
        [DidReloadScripts]
        static void OnScriptReloaded()
        { 
            GenerateAll();
        }

        static void GenerateAll()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(GenerateEventAttribute), false).Length > 0);

            foreach (var type in types)
            {
                var attr = (GenerateEventAttribute)type
                    .GetCustomAttributes(typeof(GenerateEventAttribute), false)
                    .First();
                if (!attr.GenerateClass.IsGenericType)
                {
                    Debug.Log($"[EventGen] {attr.GenerateClass} は GenericTypeではありません"); 
                    continue;
                }
                // ■ 制約チェック
                if (attr.RequireScriptableObject &&
                    !typeof(ScriptableObject).IsAssignableFrom(attr.GenerateClass))
                {
                    Debug.LogError($"[EventGen] {attr.GenerateClass} は ScriptableObject を継承していません");
                    continue;
                }
                string GenerateClass = attr.GenerateClass.Name;
                int index = GenerateClass.IndexOf("`");
                string GenerateClassName = GenerateClass.Substring(0,index);

                string className = string.IsNullOrEmpty(attr.ClassName)
                    ? $"{type.Name}Event"
                    : attr.ClassName;

                string folder = string.IsNullOrEmpty(attr.Folder)
                    ? "Assets/Scripts/Generated"
                    : attr.Folder;

                EnsureFolder(folder);

                string path = Path.Combine(folder, className + ".cs");

                // ■ 重複チェック（強化）
                if (File.Exists(path) || AlreadyExists(className))
                    continue;

                string code =
$@"using UnityEngine;
[CreateAssetMenu(menuName = ""GameEvents/{className}"")]
public class {className} : {GenerateClassName}<{type.FullName}>
{{
}}
";

                File.WriteAllText(path, code);
            }

            AssetDatabase.Refresh();
        }

        static bool AlreadyExists(string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Any(t => t.Name == className);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] split = path.Split('/');
            string current = split[0];

            for (int i = 1; i < split.Length; i++)
            {
                string next = current + "/" + split[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, split[i]);
                }
                current = next;
            }
        }
    }
}
#endif