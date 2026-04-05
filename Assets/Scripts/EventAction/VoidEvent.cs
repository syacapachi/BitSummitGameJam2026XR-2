using System;
using UnityEngine;
[CreateAssetMenu(fileName = "VoidEvent", menuName = "GameEvents/VoidEventSO", order = 1)]
public class VoidEventSO : ScriptableObject, IResisterable<Action>, IInvokable
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
