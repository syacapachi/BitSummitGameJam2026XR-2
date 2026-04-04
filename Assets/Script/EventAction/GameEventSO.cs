using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// イベントを表すScriptableObjectクラス。イベントの発生を管理し、登録されたリスナーに通知するためのクラスです。
/// </summary>
[CreateAssetMenu(fileName = "GameEvent", menuName = "ScriptableObjects/GameEventSO", order = 1)]
public class GameEventSO : ScriptableObject,IResisterable<IInvokable>,IInvokable
{
    private List<IInvokable> invokables = new();
       
    public void Register(IInvokable invokable)
    {
        if (!invokables.Contains(invokable))
        {
            invokables.Add(invokable);
        }
    }

    public void Unregister(IInvokable invokable)
    {
        if (invokables.Contains(invokable))
        {
            invokables.Remove(invokable);
        }
    }
    public void Invoke()
    {
        foreach (var invokable in invokables)
        {
            invokable.Invoke();
        }
    }
}
