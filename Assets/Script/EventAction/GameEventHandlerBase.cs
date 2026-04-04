using UnityEngine;

public class GameEventHandlerBase : MonoBehaviour,IInvokable
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
