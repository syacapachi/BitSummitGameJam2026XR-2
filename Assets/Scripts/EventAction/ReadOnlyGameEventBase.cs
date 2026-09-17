using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
/// <summary>
/// 引数を読み取り専用の参照で作る
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="value"> 読み取り専用の引数 </param>
public delegate void ReadOnlyEventListener<T>(in T value);
/// <summary>
/// イベントを表すScriptableObjectクラス。イベントの発生を管理し、登録されたリスナーに通知するためのクラスです。
/// ただし関数の引数が読み取り専用です。
/// </summary>
public class ReadOnlyGameEventBase<T> : ScriptableObject, IResisterable<ReadOnlyEventListener<T>>, IInvokable<T>
{
    private T lastValue;
    /// <summary>
    /// eventではなくリストを使うことで、購読、購読解除時の再構築コストを下げる。
    /// </summary>
    private readonly List<ReadOnlyEventListener<T>> listeners = new();
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
        listeners.Clear();
    }
    /// <summary>
    /// 関数がイベントを購読します。
    /// 指定したクラス・構造体・インターフェースなどのイベントが発火されたときに呼ばれます。
    /// </summary>
    /// <param name="invokable"> 登録する関数 </param>
    /// <remarks> また、関数を登録した数まで呼ばれます。 </remarks>
    public void Register(ReadOnlyEventListener<T> invokable)
    {
        listeners.Add(invokable);
    }
    /// <summary>
    /// 関数のイベント購読を解除します。
    /// 指定したクラス・構造体・インターフェースなどのイベントが発火されたときに呼ばれます。
    /// </summary>
    /// <param name="invokable"> 購読解除する関数 </param>

    public void Unregister(ReadOnlyEventListener<T> invokable)
    {
        listeners.Remove(invokable);
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
        for (int i = 0; i < listeners.Count; i++)
        {
            listeners[i](in value);
        }
    }

    // 演算子オーバーロード
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyGameEventBase<T> operator +(ReadOnlyGameEventBase<T> eventBase, ReadOnlyEventListener<T> invokable)
    {
        eventBase.Register(invokable);
        return eventBase;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyGameEventBase<T> operator -(ReadOnlyGameEventBase<T> eventBase, ReadOnlyEventListener<T> invokeable)
    {
        eventBase.Unregister(invokeable);
        return eventBase;
    }
}