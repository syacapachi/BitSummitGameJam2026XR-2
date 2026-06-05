#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using Unity.Netcode;
    using Unity.Netcode.Editor;
    using UnityEditor;

    /// <summary>
    /// [OnInspectorButton]属性を持つメソッドを、Inspectorにボタンとして表示。
    /// NetworkBehaviour 対応版。
    /// NetworkBehaviourを継承したクラスごとにインスタンスが生成される。
    /// </summary>
    [CustomEditor(typeof(NetworkBehaviour), true)]
    public class NetCodeButtonEditor : NetcodeEditorBase<NetworkBehaviour>
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            //base.OnInspectorGUI(); //これを呼ぶと、全てのフィールドが描画される。DrawDefaultInspector()と同様。
            //通常のインスペクター描画を行う。これを呼ばないと、通常のフィールドが表示されない。
            DrawDefaultInspector();
            //インスペクター上に関数を呼び出すためのボタンを描画する。対象のオブジェクトの型をリフレクションで調べて、[OnInspectorButton]属性が付いているメソッドを探し、ボタンを表示する。
            OnInstectorButtonDrawLogic.DrawInspectorButtons(target);

            // ネストしたScriptableObjectを再帰的に描画
            NestesObjectDrawLogic.DrawNestedScriptableObject(target);
            //変更を保存
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif