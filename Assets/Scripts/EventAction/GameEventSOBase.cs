using System;
using UnityEngine;
/// <summary>
/// イベントを表すScriptableObjectクラス。イベントの発生を管理し、登録されたリスナーに通知するためのクラスです。
/// </summary>
public class GameEventSOBase<T> : ScriptableObject,IResisterable<Action<T>>,IInvokable<T>
{
    private T lastValue;
    private event Action <T> linsteners;
    /// <summary>
    /// 最後に送ったイベントの状態を覚えておきます。
    /// </summary>
    public T CurrentValue => lastValue;
    void OnEnable()
    {
        lastValue = default;
    }

    public void Register(Action<T> invokable)
    {
        linsteners += invokable;
    }

    public void Unregister(Action<T> invokable)
    {
        linsteners -= invokable;
    }
    public void Invoke(T value = default)
    {
        lastValue = value;
        linsteners?.Invoke(value);
    }
}
/// <summary>
/// 因数なしバージョン
/// </summary>
public class GameEventSOBase : ScriptableObject, IResisterable<Action>, IInvokable
{
    private event Action linsteners;

    public void Register(Action invokable)
    {
        linsteners += invokable;
    }

    public void Unregister(Action invokable)
    {
        linsteners -= invokable;
    }
    public void Invoke()
    {
        linsteners?.Invoke();
    }
}