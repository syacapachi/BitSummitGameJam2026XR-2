using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
/// <summary>
/// sealedクラスはこれ以上継承できないクラス。
/// 設計を固定したい時に使う。
/// </summary>
public sealed class LogScope : IEquatable<LogScope>, IDisposable
{
    /// <summary>
    /// [Flags]
    /// Enumをビットフラグとして扱えるようにする属性。
    /// [通常+警告」のような複数状態を同時に保持できる。
    /// </summary>
    [Flags]
    public enum LogState : int { None = 0, Normal = 1 << 0, Warning = 1 << 1, Error = 1 << 2, Exception = 1 << 3 }
    /// <summary>
    /// ログに使う呼び出し元
    /// </summary>
    private UnityEngine.Object _object = null;

    /// <summary>
    /// プロパティ
    /// フィールドを安全に公開するための仕組み。
    /// get/set時に処理を追加できる。
    /// </summary>
    public LogState State { get; private set; } = LogState.Normal;

    /// <summary>
    /// 読み取り専用プロパティ
    /// フィールドを安全に公開するための仕組み。
    /// </summary>
    public int Count { get; private set; } = 0;

    /// <summary>
    /// エラーがあるかどうか
    /// </summary>
    public bool HasError => (State & LogState.Error) != 0;
    /// <summary>
    /// 出力する文字の保存用
    /// +で文字列を繋ぐより高速かつ省メモリに文字列を構築できる。
    /// </summary>
    private readonly StringBuilder _stringBuilder = new StringBuilder();

    private static readonly ObjectPool<LogScope> pool = new(
        createFunc: () => new LogScope(),
        actionOnGet: null,
        actionOnRelease: OnRelease,
        actionOnDestroy: null,
        defaultCapacity: 10,
        maxSize: 100
        );
    private static void OnRelease(LogScope builder)
    {
        builder._stringBuilder.Clear();
        builder.Count = 0;
        builder._object = null;
        builder.State = LogState.Normal;
    }
    /// <summary>
    /// AsyncLocalスレッドごとに作られる構造でスレッドセーフに
    /// Stack<DebugLogBuilderThreadSafe> でビルダーを入れ子にできる。
    /// </summary>
    private static readonly AsyncLocal<Stack<LogScope>> scopeStack = new();
    /// <summary>
    /// 今有効なビルダー
    /// </summary>
    private static LogScope ActionScope
    {
        get
        {
            var stack = scopeStack.Value;
            return (stack != null && stack.Count > 0)
                ? stack.Peek()
                : null;
        }
    }
    /// <summary>
    /// プライベートコンストラクタ
    /// 外部から new できません。
    /// </summary>
    private LogScope(UnityEngine.Object _object = null) { this._object = _object; }
    // ================ 内部ロジック ===================//
    private void AppendInternal(string message, LogState state)
    {
        State |= state;
        Count++;
        _stringBuilder.AppendLine(message);
    }

    // ================ 公開関数 ===================//

    /// <summary>
    /// Creates or retrieves the context instance of DebugLogBuilder.
    /// </summary>
    /// <param name="_object">The UnityEngine.Object to associate with the DebugLogBuilder instance.</param>
    /// <returns>The context instance of DebugLogBuilder.</returns>
    public static LogScope Create(UnityEngine.Object _object = null)
    {
        scopeStack.Value ??= new();
        var scope = pool.Get();
        scope._object = _object;
        scopeStack.Value.Push(scope);
        return scope;
    }
    private static LogScope Append(string message, LogState state, Action<string> fallback)
    {
        if (ActionScope is LogScope logScope)
        {
            logScope.AppendInternal(message, state);
            return logScope;
        }
        fallback(message);
        return null;
    }
    private static LogScope Append(string[] messages, LogState state, Action<string> fallback)
    {
        if (ActionScope is LogScope logScope)
        {
            foreach (string message in messages)
            {
                logScope.AppendInternal(message, state);
            }
            return logScope;
        }
        foreach (string message in messages)
        {
            fallback(message);
        }
        return null;
    }
    public static LogScope Log(string message)
    {
        return Append(message, LogState.Normal, Debug.Log);
    }
    public static LogScope Warning(string message)
    {
        return Append(message, LogState.Warning, Debug.LogWarning);
    }
    public static LogScope Error(string message)
    {
        return Append(message, LogState.Error, Debug.LogError);
    }
    public static LogScope LogException(Exception exception)
    {
        return Append(exception.ToString(), LogState.Exception, Debug.LogError);
    }
    // ================ params ===================//
    public static LogScope Log(params string[] messages)
    {
        return Append(messages, LogState.Normal, Debug.Log);
    }
    public static LogScope Warning(params string[] messages)
    {
        return Append(messages, LogState.Warning, Debug.LogWarning);
    }
    public static LogScope Error(params string[] messages)
    {
        return Append(messages, LogState.Error, Debug.LogError);
    }
    // ================ CallerMemberName ===================//
    // 実行時に、呼び出した関数、ファイル、どの行から呼んだかをコンパイラが補完してくれる。
    public static LogScope LogWithCaller(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0, [CallerFilePath] string path = "")
    {
        return Append($"[{caller}] {message} (at {path}:{line})", LogState.Normal, Debug.Log);
    }
    public static LogScope WarningWithCaller(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0, [CallerFilePath] string path = "")
    {
        return Append($"[{caller}] {message} (at {path}:{line})", LogState.Warning, Debug.LogWarning);
    }
    public static LogScope ErrorWithCaller(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0, [CallerFilePath] string path = "")
    {
        return Append($"[{caller}] {message} (at {path}:{line})", LogState.Error, Debug.LogError);
    }
    /// <summary>
    /// 出力関数。
    /// </summary>
    public void Flush()
    {
        if (_stringBuilder.Length == 0) return;
        if ((State & LogState.Exception) != 0)
        {
            Debug.LogException(new Exception(_stringBuilder.ToString()), _object);
        }
        else if ((State & LogState.Error) != 0)
        {
            Debug.LogError(_stringBuilder.ToString(), _object);
        }
        else if ((State & LogState.Warning) != 0)
        {
            Debug.LogWarning(_stringBuilder.ToString(), _object);
        }
        else
        {
            Debug.Log(_stringBuilder.ToString(), _object);
        }
    }


    // ================ 等価関数のオーバーライド ===================//

    public bool Equals(LogScope other)
    {
        if (ReferenceEquals(other, null))
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
        return _stringBuilder.ToString();
    }

    // ================ using サポート ===================//
    // using(var object){}を抜けた際自動でDispose()が呼ばれる。
    public void Dispose()
    {
        // エラーが起きるかもしれないときに使う。
        try
        {
            Flush();
        }
        // エラーが起きても起きなくても必ず実行される。
        finally
        {
            var stack = scopeStack.Value;
            var scope = stack.Pop();
            pool.Release(scope);
            if (stack.Count == 0)
            {
                scopeStack.Value = null;
            }
        }
    }


    // ================ 演算子オーバーロード ===================//
    // + や << などの演算子に独自の意味を持たせられる

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // 関数をインライン展開されやすくする属性
    // 低レベルの高速化に使う。

    /// <summary>
    /// ビルダーに通常ログを追加します。
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LogScope operator +(LogScope builder, string message)
    {
        builder.AppendInternal(message, LogState.Normal);
        return builder;
    }

    /// <summary>
    /// stringへのキャスト演算子です。
    /// implicitだと自動変換
    /// explicitだと明示的変換です.
    /// </summary>
    /// <param name="builder"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(LogScope builder)
    {
        return builder._stringBuilder.ToString();
    }
}