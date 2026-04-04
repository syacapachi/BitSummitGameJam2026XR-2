using Unity.Netcode;
using UnityEngine;

public class NetCodeGameEventHandlerBase : NetworkBehaviour,IInvokable
{
    [SerializeField] private GameEventSO gameEvent;
    protected virtual void OnEnable()
    {
        gameEvent.Register(this);
    }
    protected virtual void OnDisable()
    {
        gameEvent.Unregister(this);
    }
    public void Invoke()
    {

    }
}
