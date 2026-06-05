#if UNITY_EDITOR

namespace Syacapachi.Editor
{
    using UnityEditor;

    /// <summary>
    /// [OnInspectorButton]属性を持つメソッドを、Inspectorにボタンとして表示。
    /// UnityEngine.Objectを継承したクラスごとにインスタンスが生成される。
    /// MonoBehaviour / ScriptableObject 両対応版。
    /// ネストした ScriptableObjectも再帰的に描画。
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class OnInspectorButtonEditor : Editor
    {
        //インスペクターで触っているUnityEngine.Objectが更新されたらこれらは呼ばれる。
        //インスペクター内のMonobehaviourとかの割り当てを変えた場合も呼ばれている。
        //呼ばれる。順に上から描いた

        ////前参照してたやつ。
        //private void OnDisable()
        //{
        //    //キャッシュの明示的消去
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} OnDisabled", target);
        //}
        //private void OnDestroy()
        //{
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} OnDestroy", target);
        //}

        ////新しい参照したやつ
        //private void Reset()
        //{
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} Reset", target);
        //}
        //private void Awake()
        //{
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} Awake", target);
        //}
        //private void OnEnable()
        //{
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} OnEnabled", target);
        //}
        

        ////分からない
        //private void OnValidate()
        //{
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} Validate", target);
        //}
        //private void OnSceneGUI()
        //{
        //    Debug.Log($"{nameof(OnInspectorButtonEditor)} OnSceneGUI", target);
        //}
        

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