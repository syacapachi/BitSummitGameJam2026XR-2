#if UNITY_EDITOR
namespace Syacapachi.Editor
{
    using System;
    using System.Collections.Generic;
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
        [Flags]
        enum OptionalDrawLogic
        {
            None = 0,
            OnInstectorButtonDrawLogic = 1 << 0,
            DrawNestedScriptableObject = 1 << 1,
        }
        //staticやreadoblyは、アセンブリロード時(スクリプト編集後など)や、Play時に再生成される。
        //正しく言えば、Unityがシリアライズできるデータは、アセンブリロード前に一時退避しアセンブリロード後に再生成&代入される仕様。
        private static readonly Dictionary<UnityEngine.Object, OptionalDrawLogic> drawLogicCache = new();

        static NetCodeButtonEditor()
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

            if (!drawLogicCache.TryGetValue(currentTarget, out var drawLogic))
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

            drawLogicCache[currentTarget] = nextDrawLogic;

            //変更を保存
            if (serializedObject.ApplyModifiedProperties())
            {
                drawLogicCache.Remove(currentTarget);
            }
        }
    }
}
#endif
