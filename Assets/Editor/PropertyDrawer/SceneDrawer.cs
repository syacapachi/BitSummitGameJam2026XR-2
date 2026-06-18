#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawer
    {
        private static readonly GUIContent warningLabel = new GUIContent("Use with string fields only.", "string only");
        private static readonly Dictionary<string, string> sceneNameToPathCache = new();
        private static readonly Dictionary<string, SceneAsset> sceneAssetCache = new();
        private static readonly Dictionary<string,bool> isBuildSceneCache = new();
        static SceneDrawer()
        {
            sceneAssetCache.Clear();
            isBuildSceneCache.Clear();
            ReBuildCache(sceneNameToPathCache);
        }
        // 更新時に、名前とパスを確保(若干重くなる)
        static void ReBuildCache(Dictionary<string, string> nametoPathDic)
        {
            nametoPathDic.Clear();
            GUID[] guids = AssetDatabase.FindAssetGUIDs("t:Scene");
            foreach (GUID guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ReadOnlySpan<char> buffer = path.AsSpan();
                // Assets/Scene/scene.unity の最後の/ + 1
                int startIndex = buffer.LastIndexOf('/') + 1; 
                // 最後の .
                int endIndex = buffer.LastIndexOf('.');
                string name = buffer.Slice(startIndex, endIndex - startIndex).ToString();
                nametoPathDic[name] = path;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                SceneAttribute attr = (SceneAttribute)attribute;
                EditorGUI.BeginProperty(position, label, property);

                SceneAsset fromScene = GetSceneAsset(property.stringValue);
                //propatyがStringでもこのようにして描画できる。
                var newScene = EditorGUI.ObjectField(position, label, fromScene, typeof(SceneAsset), false) as SceneAsset;
                if (newScene == null)
                {
                    property.stringValue = "";
                }
                // これを使うことで、ビルド時に含まれているシーンだけにできる。
                else if (!attr.BuildSettingsSceneOnly || IsBuildSceneAsset(newScene.name))
                {
                    ResistScene(newScene.name, newScene);
                    property.stringValue = newScene.name;
                }
                else
                {
                    property.stringValue = "";
                }
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label, warningLabel);
            }
        }
        private static bool IsBuildSceneAsset(string sceneObjectName)
        {
            if (string.IsNullOrEmpty(sceneObjectName)) return false;
            if(!isBuildSceneCache.TryGetValue(sceneObjectName,out bool isbuildScene))
            {
                isbuildScene = false;
                //エディターで登録されている使われているシーンを検索
                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                {
                    EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                    if (scene.path.IndexOf(sceneObjectName) != -1)
                    {
                        isbuildScene = true;
                        break;
                    }
                }
                isBuildSceneCache[sceneObjectName] = isbuildScene;
            }
            
            return isbuildScene;
        }
        //ObjectFieldで貰えるものをそのままキャッシュに入れてしまう。
        private static void ResistScene(string sceneObjectName, SceneAsset sceneAsset)
        {
            if (string.IsNullOrEmpty(sceneObjectName)) { return; }
            if (sceneAsset == null) { return; }
            if (sceneAssetCache.ContainsKey(sceneObjectName)) return;
            sceneAssetCache[sceneObjectName] = sceneAsset;
        }
        private static SceneAsset GetSceneAsset(string sceneObjectName)
        {
            if (string.IsNullOrEmpty(sceneObjectName)) return null;
            if (!sceneAssetCache.TryGetValue(sceneObjectName, out SceneAsset sceneAsset))
            {
                sceneAsset = null;
                if(sceneNameToPathCache.TryGetValue(sceneObjectName,out string path))
                {
                    //必要なときだけロード
                    sceneAsset = AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset)) as SceneAsset;
                    sceneAssetCache[sceneObjectName] = sceneAsset;
                }
            }
            return sceneAsset;
        }
    }
}
#endif
