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
        //スクリプト更新時に呼ばれる
        [DidReloadScripts]
        static void OnScriptReloaded()
        { 
            GenerateAll();
        }

        static void GenerateAll()
        {
            //アセンブリからGenerateEventAttributeがついたクラス・構造体を検索
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(GenerateEventAttribute), false).Length > 0);

            foreach (var type in types)
            {
                var attr = (GenerateEventAttribute)type
                    .GetCustomAttributes(typeof(GenerateEventAttribute), false)
                    .First();
                //<T>か調べる
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
                //ジェネリック型は、NameSpace.ClassName`1 のように型が1つ入ることを書くので`以降を無視。
                string GenerateClass = attr.GenerateClass.Name;
                int index = GenerateClass.IndexOf("`");
                string GenerateClassName = GenerateClass.Substring(0,index);

                string className = string.IsNullOrEmpty(attr.ClassName)
                    ? 
                    attr.IsArray ? $"{type.Name}ArrayEvent" : $"{type.Name}Event"
                    : attr.ClassName;

                string folder = string.IsNullOrEmpty(attr.Folder)
                    ? "Assets/Scripts/Generated"
                    : attr.Folder;

                EnsureFolder(folder);

                string path = Path.Combine(folder, className + ".cs");

                // ■ 重複チェック（強化）
                if (File.Exists(path) || AlreadyExists(className))
                    continue;
                //内部クラスは、NameSpace.SampleClass+InlineClassのように+で表されるが、書くときは.なので、書き換える。
                string inlineClassName = type.FullName.Replace("+", ".");
                if (attr.IsArray)
                {
                    inlineClassName += "[]";
                }
                string code =
$@"using UnityEngine;
[CreateAssetMenu(menuName = ""GameEvents/{className}"")]
public class {className} : {GenerateClassName}<{inlineClassName}>
{{
}}
";

                File.WriteAllText(path, code);
                Debug.Log($"[{nameof(AutoEventGenerator)}]Create new Script At :{path},Name = {className} extends {GenerateClassName}");
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