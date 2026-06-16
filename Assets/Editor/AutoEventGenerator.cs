#if UNITY_EDITOR
namespace Syacapachi.util
{
    using Syacapachi.Attribute;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;
    //呼ばれすぎる
    //[InitializeOnLoad]
    public static class AutoEventGenerator
    {
        static AutoEventGenerator()
        {
            //コンストラクタない呼ぶ必要ない(2回呼ぶことになる)
            //GenerateAll();
        }
        //スクリプト更新時に呼ばれる
        [DidReloadScripts]
        static void OnScriptReloaded()
        { 
            GenerateAll();
        }
        //UnityEditorのstaticは、Editor更新時に再生成される。(設定でOFFにしなければ)
        static readonly HashSet<string> cachedTypeNames = 
            new(
                //キャッシュにあるすべてのTypeの名前を登録
                TypeCache.GetTypesDerivedFrom<object>()
                .Select(t => t.Name)
            );

        static void GenerateAll()
        {
            bool generated = false;
            //アセンブリからGenerateEventAttributeがついたクラス・構造体を検索
            //var types = AppDomain.CurrentDomain.GetAssemblies()
            //    .SelectMany(a => a.GetTypes())
            //    .Where(t => t.GetCustomAttributes(typeof(GenerateEventAttribute), false).Length > 0);

            //Unity内部のキャッシュで検索高速化
            var types = TypeCache.GetTypesWithAttribute<GenerateEventAttribute>();

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<GenerateEventAttribute>(false);
                //<T>か調べる
                if (!attr.GenerateClass.IsGenericType)
                {
                    Debug.Log($"[{nameof(AutoEventGenerator)}] {attr.GenerateClass} は GenericTypeではありません"); 
                    continue;
                }
                // ■ 制約チェック
                if (attr.RequireScriptableObject &&
                    !typeof(ScriptableObject).IsAssignableFrom(attr.GenerateClass))
                {
                    Debug.LogError($"[{nameof(AutoEventGenerator)}] {attr.GenerateClass} は ScriptableObject を継承していません");
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
                generated = true;
                Debug.Log($"[{nameof(AutoEventGenerator)}]Create new Script At :{path},Name = {className} extends {GenerateClassName}");
            }
            //生成時のみリフレッシュ
            if (generated)
            {
                AssetDatabase.Refresh();
            }
        }

        static bool AlreadyExists(string className)
        {
            return cachedTypeNames.Contains(className);
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