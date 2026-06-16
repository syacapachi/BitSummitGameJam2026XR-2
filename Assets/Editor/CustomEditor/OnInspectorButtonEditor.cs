#if UNITY_EDITOR

namespace Syacapachi.Editor
{
    using System;
    using System.Collections.Generic;
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
        [Flags]
        enum OptionalDrawLogic
        {
            None = 0,
            OnInstectorButtonDrawLogic = 1 << 0,
            DrawNestedScriptableObject = 1 << 1,
        }
        //staticやreadoblyは、アセンブリロード時(スクリプト編集後など)や、Play時に再生成される。
        //正しく言えば、Unityがシリアライズできるデータは、アセンブリロード前に一時退避しアセンブリロード後に再生成&代入される仕様。
        //UnityEngine.Object.GetInstanceID()をキーとする。
        //UnityEngine.Objectの参照があるとGCできない
        private static readonly Dictionary<int, OptionalDrawLogic> drawLogicCache = new();

        static OnInspectorButtonEditor()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearDrawLogicCache;
            EditorApplication.hierarchyChanged += ClearDrawLogicCache;
            EditorApplication.projectChanged += ClearDrawLogicCache;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Selection.selectionChanged += ClearDrawLogicCache;
            Undo.undoRedoPerformed += ClearDrawLogicCache;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange _)
        {
            ClearDrawLogicCache();
        }

        private static void ClearDrawLogicCache()
        {
            drawLogicCache.Clear();
        }

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

            UnityEngine.Object currentTarget = target;
            if (currentTarget == null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (!drawLogicCache.TryGetValue(currentTarget.GetInstanceID(), out var drawLogic))
            {
                drawLogic =
                    OptionalDrawLogic.OnInstectorButtonDrawLogic |
                    OptionalDrawLogic.DrawNestedScriptableObject;
            }

            OptionalDrawLogic nextDrawLogic = OptionalDrawLogic.None;

            if ((drawLogic & OptionalDrawLogic.OnInstectorButtonDrawLogic) != 0)
            {
                //インスペクター上に関数を呼び出すためのボタンを描画する。対象のオブジェクトの型をリフレクションで調べて、[OnInspectorButton]属性が付いているメソッドを探し、ボタンを表示する。
                var result = OnInstectorButtonDrawLogic.DrawInspectorButtons(currentTarget);
                if ((result & OnInstectorButtonDrawLogic.InspectorButtonResult.DrawedButton) != 0)
                {
                    nextDrawLogic |= OptionalDrawLogic.OnInstectorButtonDrawLogic;
                }
            }

            if ((drawLogic & OptionalDrawLogic.DrawNestedScriptableObject) != 0)
            {
                // ネストしたScriptableObjectを再帰的に描画
                var result = NestesObjectDrawLogic.DrawNestedScriptableObject(currentTarget);
                if ((result & NestesObjectDrawLogic.NestedScriptableObjectResult.DrawedObject) != 0)
                {
                    nextDrawLogic |= OptionalDrawLogic.DrawNestedScriptableObject;
                }
            }

            drawLogicCache[currentTarget.GetInstanceID()] = nextDrawLogic;

            //変更を保存
            if (serializedObject.ApplyModifiedProperties())
            {
                drawLogicCache.Remove(currentTarget.GetInstanceID());
            }
        } 
    }
}
#endif
