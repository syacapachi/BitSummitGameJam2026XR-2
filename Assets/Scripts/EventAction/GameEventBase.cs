using System;
using System.Runtime.CompilerServices;
using UnityEngine;
/// <summary>
/// イベントを表すScriptableObjectクラス。イベントの発生を管理し、登録されたリスナーに通知するためのクラスです。
/// </summary>
public class GameEventBase<T> : ScriptableObject,IResisterable<Action<T>>,IInvokable<T>
{
    private T lastValue;
    private event Action <T> Listeners;
    /// <summary>
    /// 最後に送ったイベントの状態を覚えておくメモリ参照。
    /// </summary>
    public ref readonly T CurrentValue => ref lastValue;
    void OnEnable()
    {
        lastValue = default;
    }
    /// <summary>
    /// 購読全体を解除
    /// </summary>
    void OnDisable()
    {
        Listeners = null;
    }
    /// <summary>
    /// 関数がイベントを購読します。
    /// 指定したクラス・構造体・インターフェースなどのイベントが発火されたときに呼ばれます。
    /// </summary>
    /// <param name="invokable"> 登録する関数 </param>
    /// <remarks> また、関数を登録した数まで呼ばれます。 </remarks>
    public void Register(Action<T> invokable)
    {
        Listeners += invokable;
    }
    /// <summary>
    /// 関数のイベント購読を解除します。
    /// 指定したクラス・構造体・インターフェースなどのイベントが発火されたときに呼ばれます。
    /// </summary>
    /// <param name="invokable"> 購読解除する関数 </param>

    public void Unregister(Action<T> invokable)
    {
        Listeners -= invokable;
    }
    /// <summary>
    /// イベントを発火します、
    /// </summary>
    /// <param name="value"> 送信するイベントの参照コピー </param>
    /// <remarks>メインスレッド以外で読んだ場合,asyncは未定義動作です。</remarks>
    public void Invoke(in T value)
    {
        //値をコピー(refにすると寿命管理発生)
        lastValue = value;
        //ちなみに、 ActionのInvokeは参照コピーではないため、中身がコピーされます。
        //参照コピーの恩恵を受けたい場合、自前delegateを作る(すぐできる)
        Listeners?.Invoke(value);
    }

    // 演算子オーバーロード
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameEventBase<T> operator +(GameEventBase<T> eventBase, Action<T> invokable)
    {
        eventBase.Register(invokable);
        return eventBase;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameEventBase<T> operator -(GameEventBase<T> eventBase, Action<T> invokeable)
    {
        eventBase.Unregister(invokeable);
        return eventBase;
    }
}
/// <summary>
/// 因数なしバージョン
/// </summary>
public class GameEventBase : ScriptableObject, IResisterable<Action>, IInvokable
{
    private event Action Linsteners;

    public void Register(Action invokable)
    {
        Linsteners += invokable;
    }

    public void Unregister(Action invokable)
    {
        Linsteners -= invokable;
    }
    public void Invoke()
    {
        Linsteners?.Invoke();
    }

    // 演算子オーバーロード
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameEventBase operator +(GameEventBase eventBase, Action invokable)
    {
        eventBase.Register(invokable);
        return eventBase;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameEventBase operator -(GameEventBase eventBase, Action invokeable)
    {
        eventBase.Unregister(invokeable);
        return eventBase;
    }
}