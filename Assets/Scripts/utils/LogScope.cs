using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections;



#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Debug.Log()の代わりに使うことで、スコープ内のログを蓄積し、スコープ終了時にまとめて出力するユーティリティクラスです。
/// スレッドセーフ&スコープのネストができます。
/// </summary>
public sealed class LogScope : IEquatable<LogScope>, IDisposable
{
    #region enums
    /// <summary>
    /// [Flags]
    /// Enumをビットフラグとして扱えるようにする属性。
    /// [通常+警告」のような複数状態を同時に保持できる。
    /// </summary>
    [Flags]
    public enum LogState : int { None = 0, Normal = 1 << 0, Warning = 1 << 1, Error = 1 << 2, Exception = 1 << 3 }
    /// <summary>
    /// 自信を管理するStack
    /// </summary>
    private enum ScopeMode {ThreadStack, Coroutine}
    #endregion
    /// <summary>
    /// 実行時定数
    /// </summary>
    private static readonly string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
    // フィールドのロック専用
    private readonly object lockObject = new object();
    // 静的ObjectPool専用 ロックの単位を小さくする。
    private static readonly object poolLockObject = new object();
    #region private instance Field
    /// <summary>
    /// ログに使う呼び出し元
    /// </summary>
    private UnityEngine.Object _object = null;
    /// <summary>
    /// ログのラベル
    /// </summary>
    private string label = string.Empty;
    /// <summary>
    /// yield return をまたいだため外されたか
    /// </summary>
    private bool isDetached = false;
    /// <summary>
    /// 解放されたかのフラグ
    /// </summary>
    private int isDisposed = 0;
    /// <summary>
    /// 自身がStackに積まれる直前のScopeの深さ(0~n)
    /// </summary>
    private int depth = 0;
    /// <summary>
    /// 蓄えたログの数
    /// </summary>
    private int count = 0;
    /// <summary>
    /// 最も重いログレベル
    /// </summary>
    private LogState state = LogState.Normal;
    /// <summary>
    /// 管理Stack
    /// </summary>
    private ScopeMode scopeMode = ScopeMode.ThreadStack;
    #endregion
    /// <summary>
    /// プロパティ
    /// フィールドを安全に公開するための仕組み。
    /// get/set時に処理を追加できる。
    /// </summary>
    public LogState State { get { lock (lockObject) return state; }}

    /// <summary>
    /// 読み取り専用プロパティ
    /// フィールドを安全に公開するための仕組み。
    /// </summary>
    public int Count { get { lock (lockObject) return count; } }

    /// <summary>
    /// エラーがあるかどうか
    /// </summary>
    public bool HasError { get { lock(lockObject) return (state & LogState.Error) != 0; } }
    /// <summary>
    /// 出力する文字の保存用
    /// +で文字列を繋ぐより高速かつ省メモリに文字列を構築できる。
    /// </summary>
    private readonly StringBuilder _stringBuilder = new StringBuilder();
    #region ObjectPool
    /// <summary>
    /// オブジェクトのリサイクルでメモリを節約する仕組み
    /// </summary>
    private static readonly ObjectPool<LogScope> pool = new(
        createFunc: () => new LogScope(),
        actionOnGet: OnGet,
        actionOnRelease: OnRelease,
        actionOnDestroy: null,
        defaultCapacity: 10,
        maxSize: 100
        );
    
    /// <summary>
    /// オブジェクトが取り出されたときに呼ばれる。
    /// フラグの初期化
    /// </summary>
    /// <param name="scope"></param>
    private static void OnGet(LogScope scope)
    {
        scope.isDisposed = 0;
    }
    /// <summary>
    /// オブジェクトが回収されるときに呼ばれる
    /// 初期化
    /// </summary>
    /// <param name="scope"></param>
    private static void OnRelease(LogScope scope)
    {
        scope._stringBuilder.Clear();
        scope.count = 0;
        scope.depth = 0;
        scope.isDetached = false;
        scope.label = string.Empty;
        scope._object = null;
        scope.state = LogState.Normal;
    }
    /// <summary>
    /// オブジェクトプールから安全にLogScopeを取得します。
    /// </summary>
    /// <returns></returns>
    private static LogScope RentScope()
    {
        // ObjectPoolはスレッドセーフではないため排他制御する。
        lock (poolLockObject)
        {
            return pool.Get();
        }
    }
    /// <summary>
    /// LogScopeを安全に返却する。
    /// </summary>
    /// <param name="scope"></param>
    private static void ReleaseScope(LogScope scope)
    {
        lock (poolLockObject)
        {
            pool.Release(scope);
        }
    }
    #endregion
    #region ScopeStack
    /// <summary>
    /// AsyncLocalスレッドごとに作られる構造でスレッドセーフに
    /// Stack<DebugLogBuilderThreadSafe> でビルダーを入れ子にできる。
    /// </summary>
    private static readonly AsyncLocal<Stack<LogScope>> scopeStack = new();
    /// <summary>
    /// 今有効なビルダー
    /// </summary>
    private static LogScope ActiveScope
    {
        get
        {
            var stack = scopeStack.Value;
            if (stack != null && stack.Count > 0)
                return stack.Peek();
            
            return null; 
        }
    }
    private static void Push(LogScope scope)
    {
        if (ActiveScope != null)
        {
            ActiveScope.AppendInternal($">>>>>>>>>>>> [{scope.label}]", LogState.None);
        }
        scopeStack.Value.Push(scope);
    }
    private static LogScope Pop()
    {
        if(TryPop(out LogScope scope))
        {
            return scope;
        }
        return null;
    }
    private static bool TryPop(out LogScope poped)
    {
        poped = null;
        if(scopeStack.Value == null) return false;
        if(scopeStack.Value.Count == 0) return true;
        poped = scopeStack.Value.Pop();
        if (ActiveScope != null)
        {
            ActiveScope.AppendInternal($"<<<<<<<<<<<< [{poped.label}]", LogState.None);
        }
        return poped != null;
    }
    #endregion
    /// <summary>
    /// プライベートコンストラクタ
    /// 外部から new できません。
    /// </summary>
    private LogScope(UnityEngine.Object _object = null) { this._object = _object; }
    // ================ 内部ロジック ===================//
    #region Inner Logics
    private void AppendInternal(string message, LogState newState)
    {
        lock (lockObject)
        {
            if (newState != LogState.None)
            {
                count++;
                state |= newState;
            }
            _stringBuilder.AppendLine(message);
        }
    }
    private void DisposeInternal(bool flushSucceeded)
    {
        if (scopeMode == ScopeMode.Coroutine)
        {
            ReleaseScope(this);
            return;
        }
        if (isDetached)
        {
            Debug.LogWarning("LogScope crossed yield return.\n Stack consistency recovered automatically.");
            ReleaseScope(this);
            return;
        }

        //取り出す
        if (!TryPop(out LogScope scope))
        {
            // 失敗した場合はStackチェック
            Stack<LogScope> stack = scopeStack.Value;
            // Dispose失敗 stackがnull
            if (stack == null)
            {
                Debug.LogError("LogScope.Dispose() failed\n stack is null");
            }
            // Dispose失敗 stackが空
            else if (stack.Count == 0)
            {
                Debug.LogError("LogScope.Dispose() failed\n stack is empty");
            }
            isDisposed = 0;
            return;
        }
        
        // Dispose失敗 順番違う
        if (!ReferenceEquals(scope, this))
        {
            Debug.LogError(
                "LogScope.Dispose() failed.\n" +
                $"CurrentDepth {scope.depth}.\n" +
                $"DisposeDepth {this.depth}.\n" +
                $"Current {scope}.\n" +
                $"Dispose {this}.\n" +
                "Check whether nested LogScopes are disposed in reverse order.");
        }
        // Flush()失敗
        else if (!flushSucceeded)
        {
            Debug.LogError("LogScope.Dispose() failed because Flush() threw an exception.");
        }
        //何もなければ返却
        else
        {
            ReleaseScope(scope);
        }
    }
    private static void Append(string message, LogState state, Action<string> fallback)
    {
#if UNITY_EDITOR
        if (LogScopeConfig.IsTestBuildEnv && ((int)LogState.Exception >> LogScopeConfig.LogLevel) > (int)state) return;
#endif
        if (ActiveScope is LogScope logScope)
        {
            logScope.AppendInternal(message, state);
            return;
        }
        fallback(message);
    }
    private static void Append(string[] messages, LogState state, Action<string> fallback)
    {
#if UNITY_EDITOR
        if (LogScopeConfig.IsTestBuildEnv && ((int)LogState.Exception >> LogScopeConfig.LogLevel) > (int)state) return;
#endif
        if (ActiveScope is LogScope logScope)
        {
            foreach (string message in messages)
            {
                logScope.AppendInternal(message, state);
            }
            return;
        }
        foreach (string message in messages)
        {
            fallback(message);
        }
    }
    private static LogScope CreateCoroutineScope(UnityEngine.Object context = null, string label = null)
    {
        LogScope scope = RentScope();
        scopeStack.Value ??= new Stack<LogScope>();
        scope.depth = scopeStack.Value.Count;
        scope._object = context;
        scope.scopeMode = ScopeMode.Coroutine;
        scope.label = label ?? $"depth{scope.depth}";
        scope.AppendInternal($"[{scope.label}]", LogState.None);
        return scope;
    }
    #endregion
    // ================ 公開関数 ===================//
    #region Create
    /// <summary>
    /// LogScopeインスタンスを作成します。
    /// 通常はこちらを使用してください。
    /// </summary>
    /// <param name="_object">Debug logに渡す UnityEngine.Object</param>
    /// <returns> 現在のログを蓄積するLogScopeインスタンス </returns>
    public static LogScope Create(UnityEngine.Object _object = null, string label = null)
    {
        LogScope scope = RentScope();
        scopeStack.Value ??= new Stack<LogScope>();
        scope.depth = scopeStack.Value.Count;
        scope._object = _object;
        scope.scopeMode = ScopeMode.ThreadStack;
        scope.label = label ?? $"depth{scope.depth}";
        scope.AppendInternal($"[{scope.label}]", LogState.None);
        Push(scope);
        
        return scope;
    }
    /// <summary>
    /// Task.Run()など別ExecutionContextでネストしたLogScopeを作成します。
    /// 通常のCreate()では親ScopeのStackを共有してしまうため、
    /// Stackをコピーして親子を分離します。
    /// </summary>
    /// <param name="_object">Debug.Logに渡すUnityEngine.Object</param>
    /// <returns>現在のログを蓄積するLogScopeインスタンス</returns>
    public static LogScope CreateIsolated(UnityEngine.Object _object = null, string label = null)
    {
        LogScope scope = RentScope();
        /*
        * AsyncLocalはExecutionContextごとに値を保持します。
        * しかしValueが参照型(Stack<LogScope>)の場合、
        * Task開始時はStackの参照だけがコピーされます。
        *
        *      Main
        *      Stack
        *       └ ScopeA
        *          ▲
        *          │（同じStackを参照）
        *      Task
        *
        * この状態でTask側がPush()/Pop()すると、
        * 親のStackまで変更されてしまいます。
        *
        * そこでStack自体をコピーして、
        * 親Taskと子Taskが別々のStackを持つようにします。
        */
        var inherited = scopeStack.Value;
        scopeStack.Value =
            inherited == null
            ? new Stack<LogScope>()
            : new Stack<LogScope>(inherited.Reverse());
        scope.depth = scopeStack.Value.Count;
        scope._object = _object;
        scope.scopeMode = ScopeMode.ThreadStack;
        scope.label = label ?? $"depth{scope.depth}";
        scope.AppendInternal($"[{scope.label}]", LogState.None);
        Push(scope);

        return scope;
    }
    #endregion
    #region Coroutine
    /// <summary>
    /// コルーチン専用ラッパー
    /// コルーチン内で、Create()するのではなく、
    /// これをStartCorutine()してください。
    /// </summary>
    /// <param name="routine"> コルーチン本体 </param>
    /// <param name="context">Debug Log()のコンテキスト</param>
    /// <returns> コルーチンの返り値 </returns>
    public static IEnumerator Run(IEnumerator routine, UnityEngine.Object context = null, string label = null)
    {
        using LogScope scope = CreateCoroutineScope(context, label);
        // stack
        int scopeDepth = scope.depth;
        while (true)
        {
            bool next;
            // コルーチンスタックに積む。
            Push(scope);
            try
            {
                //ここで行われるログを回収
                next = routine.MoveNext();
            }
            finally
            {
                // yield return をまたいだものを消す
                while (scopeDepth + 1 < scopeStack.Value.Count)
                {
                    var leaked = Pop();
                    leaked.isDetached = true;
                    Debug.LogError(
                    "LogScope.RunRecrucive() failed.\n" +
                    $"RequireDepth {scopeDepth}\n" +
                    $"CurrentDepth {leaked.depth}.\n" +
                    $"Leaked \n{leaked}.\n" +
                    "Check whether nested LogScopes are disposed in reverse order.");
                }
                // 追加したものが外れるはず。
                Pop();
            }

            if (!next)
                yield break;
            
            //自分を呼んだやつ
            yield return routine.Current;
        }
    }
    /// <summary>
     /// コルーチン専用ラッパー
     /// コルーチン内で、Create()するのではなく、
     /// これをStartCorutine()してください。
     /// 内部にIEnumratorがある場合、再帰的にラッパークラスを呼びます。
     /// (これが底にある時、StartCoroutine()の方向へラッパー)
     /// </summary>
     /// <param name="routine"> コルーチン本体 </param>
     /// <param name="context">Debug Log()のコンテキスト</param>
     /// <returns> コルーチンの返り値 </returns>
    public static IEnumerator RunRecrucive(IEnumerator routine, UnityEngine.Object context = null, string label = null)
    {
        using LogScope scope = CreateCoroutineScope(context, label);
        int scopeDepth = scope.depth;
        while (true)
        {
            bool next;
            // スタックに積む。
            Push(scope);
            try
            {
                next = routine.MoveNext();
            }
            finally
            {
                // yield return をまたいだものを消す
                while (scopeDepth + 1 < scopeStack.Value.Count)
                {
                    var leaked = Pop();
                    leaked.isDetached = true;
                    Debug.LogError(
                    "LogScope.RunRecrucive() failed.\n" +
                    $"RequireDepth {scopeDepth}\n" +
                    $"CurrentDepth {leaked.depth}.\n" +
                    $"Leaked \n{leaked}.\n" +
                    "Check whether nested LogScopes are disposed in reverse order.");
                }
                // 追加したものが外れるはず。
                Pop();
            }

            if (!next)
                yield break;

            object current = routine.Current;

            // 子供タスクも自動で追加
            if (current is IEnumerator child)
            {
                yield return RunRecrucive(child, context);
            }
            else
            {
                yield return current;
            }
        }
    }
    #endregion
    #region AddLogs
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void Log(string message)
    {
        Append(message, LogState.Normal, Debug.Log);
    }
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void Warning(string message)
    {
        Append(message, LogState.Warning, Debug.LogWarning);
    }
    [Conditional("LOGSCOPE_LEVEL1")]
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void Error(string message)
    {
        Append(message, LogState.Error, Debug.LogError);
    }
    [Conditional("LOGSCOPE_LEVEL1")]
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void LogException(Exception exception)
    {
        Append(exception.ToString(), LogState.Exception, Debug.LogError);
    }
    // ================ params ===================//
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void Log(params string[] messages)
    {
        Append(messages, LogState.Normal, Debug.Log);
    }
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void Warning(params string[] messages)
    {
        Append(messages, LogState.Warning, Debug.LogWarning);
    }
    [Conditional("LOGSCOPE_LEVEL1")]
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void Error(params string[] messages)
    {
        Append(messages, LogState.Error, Debug.LogError);
    }
    // ================ CallerMemberName ===================//
    // 実行時に、呼び出した関数、ファイル、どの行から呼んだかをコンパイラが補完してくれる。
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void LogWithCaller(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0, [CallerFilePath] string path = "")
    {
        // リッチテキストを用いてリンクを作成。
        string className = Path.GetFileNameWithoutExtension(path);
        string filePath = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
        Append($"{message} [{className}:{caller}()] (at <a href=\"{path}\"line=\"{line}\"><u>{filePath}:{line}</u></a>)", LogState.Normal, Debug.Log);
    }
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void WarningWithCaller(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0, [CallerFilePath] string path = "")
    {
        // リッチテキストを用いてリンクを作成。
        string className = Path.GetFileNameWithoutExtension(path);
        string filePath = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
        Append($"{message} [{className}:{caller}()] (at <a href=\"{path}\"line=\"{line}\"><u>{filePath}:{line}</u></a>)", LogState.Warning, Debug.LogWarning);
    }
    [Conditional("LOGSCOPE_LEVEL1")]
    [Conditional("LOGSCOPE_LEVEL2")]
    [Conditional("LOGSCOPE_LEVEL3")]
    [Conditional("UNITY_EDITOR")]
    public static void ErrorWithCaller(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0, [CallerFilePath] string path = "")
    {
        // リッチテキストを用いてリンクを作成。
        string className = Path.GetFileNameWithoutExtension(path);
        string filePath = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
        Append($"{message} [{className}:{caller}()] (at <a href=\"{path}\"line=\"{line}\"><u>{filePath}:{line}</u></a>)", LogState.Error, Debug.LogError);
    }
    #endregion
    /// <summary>
    /// 出力関数。
    /// </summary>
    public void Flush()
    {
        string output = null;
        LogState state = LogState.Normal;
        UnityEngine.Object context = null;
        lock (lockObject)
        {
            output = _stringBuilder.ToString();
            state = this.state;
            context = _object;
        }
        if (output == null) return;
        if ((state & LogState.Exception) != 0)
        {
            Debug.LogException(new Exception(output), context);
        }
        else if ((state & LogState.Error) != 0)
        {
            Debug.LogError(output, context);
        }
        else if ((state & LogState.Warning) != 0)
        {
            Debug.LogWarning(output, context);
        }
        else
        {
            Debug.Log(output, context);
        }
    }
    // ================ using サポート ===================//
    // using(var object){}を抜けた際自動でDispose()が呼ばれる。
    public void Dispose()
    {
        // isDisposedの値を出力し、参照先の値を変更 (排他制御)
        if (Interlocked.Exchange(ref isDisposed, 1) != 0)
        {
            Debug.LogError("Already Disposed");
            return;
        }
            
        bool success = false;
        // エラーが起きるかもしれないときに使う。
        try
        {
            Flush();
            success = true;
        }
        // エラーが起きても起きなくても必ず実行される。
        finally
        {
            DisposeInternal(success);
        }
    }

    #region Overrides
    // ================ 等価関数のオーバーライド ===================//

    public bool Equals(LogScope other)
    {
        if (other is null)
            return false;
        return this._object == other._object;
    }

    // ================ 基底クラスのオーバーライド ===================//
    public override bool Equals(object obj)
    {
        if (obj is LogScope builder)
            // 自作の等価関数を呼ぶ
            return builder.Equals(this);
        else return false;
    }
    public override int GetHashCode()
    {
        return _object != null ? _object.GetHashCode() : -1;
    }

    public override string ToString()
    {
        lock (lockObject)
        {
            return _stringBuilder.ToString();
        }
    }
    #endregion



    // ================ 演算子オーバーロード ===================//
    // + や << などの演算子に独自の意味を持たせられる

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // 関数をインライン展開されやすくする属性
    // 低レベルの高速化に使う。
    /// <summary>
    /// stringへのキャスト演算子です。
    /// implicitだと自動変換
    /// explicitだと明示的変換です.
    /// </summary>
    /// <param name="scope"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(LogScope scope)
    {
        lock (scope.lockObject)
        {
            return scope._stringBuilder.ToString();
        }
    }
}
#if UNITY_EDITOR
/// <summary>
/// エディター専用データに設定を読み書きするクラス。
/// 後から追加しやすい。
/// </summary>
public sealed class LogScopeEditorSettings : ScriptableSingleton<LogScopeEditorSettings>
{
    public bool isTestBuildEnv;
    public int logLevel;
}
/// <summary>
/// エディター専用とランタイムで使うものを変えるためのクラス。
/// </summary>
internal static class LogScopeConfig
{
    public static bool IsTestBuildEnv
    {
        get
        {
            return LogScopeEditorSettings.instance.isTestBuildEnv;
        }
    }
    /// <summary>
    /// 0,1,2,3
    /// </summary>
    public static int LogLevel
    {
        get
        {
            return LogScopeEditorSettings.instance.logLevel;
        }
    }
}
#endif
