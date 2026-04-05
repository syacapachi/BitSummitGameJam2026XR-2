using System;
using System.Collections.Generic;
public interface IEventBus
{
    void Publish<T>(T e);
    void Subscribe<T>(Action<T> action);
    void Unsubscribe<T>(Action<T> action);
}
/// <summary>
/// 使い方
/// 受信側は、Subscribeに受信データを入れる。
/// 発行側は、Publichにに送信データを入れる。
/// 例:銃を撃つ　-> WeaponEvent();
/// 入力クラス Publish<WeaponEvent()>();
/// 受信クラス Subscribe<WeaponEvent()>();
/// </summary>
public class EventBus
{
    private Dictionary<Type, Delegate> events = new();

    public void Publish<T>(T e)
    {
        if (events.TryGetValue(typeof(T), out var del))
        {
            (del as Action<T>)?.Invoke(e);
        }
    }

    public void Subscribe<T>(Action<T> action)
    {
        if (events.TryGetValue(typeof(T), out var del))
            events[typeof(T)] = Delegate.Combine(del, action);
        else
            events[typeof(T)] = action;
    }

    public void Unsubscribe<T>(Action<T> action)
    {
        if (events.TryGetValue(typeof(T), out var del))
            events[typeof(T)] = Delegate.Remove(del, action);
    }
}
