#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Syacapachi.Attribute;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawer
    {
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
                else if (!attr.allowBuildSettingsSceneOnly)
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
                EditorGUI.LabelField(position, label.text, "Use with string fields only.");
            }
        }
        private SceneAsset GetSceneAsset(string sceneObjectName)
        {
            if (string.IsNullOrEmpty(sceneObjectName)) return null;
            //エディターで登録されている使われているシーンを検索
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                if (scene.path.IndexOf(sceneObjectName) != -1)
                {
                    //見つかった場合、SceneAssetにして返す。
                    return AssetDatabase.LoadAssetAtPath(scene.path, typeof(SceneAsset)) as SceneAsset;
                }
            }
            Debug.Log("Scene [" + sceneObjectName + "] cannot be used. Add this scene to the 'Scenes in the Build' in the build settings.");
            return null;
        }
    }
}
#endif
