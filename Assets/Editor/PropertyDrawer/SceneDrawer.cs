#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawer
    {
        private static readonly GUIContent warningLabel = new GUIContent("Use with string fields only.", "string only");
        //private static readonly Dictionary<string, string> sceneNameToPathCache = new();
        private static readonly Dictionary<string, SceneAsset> sceneAssetCahce = new();
        //static SceneDrawer()
        //{

        //}
        static void ReBuildCache(Dictionary<string, string> nametoPathDic)
        {
            nametoPathDic.Clear();
            GUID[] guids = AssetDatabase.FindAssetGUIDs("t:Scene");
            string path = "";
            foreach (GUID guid in guids)
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.LoadAssetByGUID(guid,typeof(SceneAsset));

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
                var newScene = EditorGUI.ObjectField(position, label, fromScene, typeof(SceneAsset), false);
                if(newScene == null)
                {
                    property.stringValue = "";
                }
                else if (!attr.buildSettingsSceneOnly)
                {
                    
                    property.stringValue = newScene.name;
                }
                else
                {
                    //これを使うことで、ビルド時に含まれているシーンだけにできる。
                    var buildScene = GetSceneAsset(newScene.name);
                    if(buildScene == null)
                    {
                        property.stringValue = "";
                    }
                    else
                    {
                        property.stringValue = buildScene.name;
                    }
                }
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label, warningLabel);
            }
        }
        private static SceneAsset GetSceneAsset(string sceneObjectName)
        {
            if (string.IsNullOrEmpty(sceneObjectName)) return null;
            if (sceneAssetCahce.TryGetValue(sceneObjectName, out var sceneAsset))
            {
                return sceneAsset;
            }
            //エディターで登録されている使われているシーンを検索
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                if (scene.path.IndexOf(sceneObjectName) != -1)
                {
                    sceneAsset = AssetDatabase.LoadAssetAtPath(scene.path, typeof(SceneAsset)) as SceneAsset;
                    sceneAssetCahce[sceneObjectName] = sceneAsset;
                    //見つかった場合、SceneAssetにして返す。
                    return sceneAsset;
                }
            }
            Debug.LogWarning("Scene [" + sceneObjectName + "] cannot be used. Add this scene to the 'Scenes in the Build' in the build settings.");
            return null;
        }
    }
}
#endif
