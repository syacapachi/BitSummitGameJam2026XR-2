using System;
using UnityEngine;

public class ChangeWhenEvent : MonoBehaviour
{
    [Serializable]
    protected class State
    {
        [SerializeField] bool isInverse = false;
        [SerializeField] GameObject m_objects;
        public void ApplyWithInverse(bool enable)
        {
            if (isInverse)
            {
                enable = !enable;
            }
            Apply(enable);
        }
        public void Apply(bool enable)
        {
            m_objects.SetActive(enable);
        }
    }
    [SerializeField] protected State[] _state;
    [Header("Subscribe Event")]
    [SerializeField] BoolEvent connectionEvent;

    void Awake()
    {
        foreach(var state in _state)
        {
            state.ApplyWithInverse(connectionEvent.CurrentValue);
        }
    }

    void OnEnable()
    {
        connectionEvent.Register(OnEvent);
    }
    void OnDisable()
    {
        connectionEvent.Unregister(OnEvent);
    }
    private void OnEvent(bool enable)
    {
        //Debug.Log($"[{nameof(ChangeWhenEvent)}][{gameObject.name}] :Recived {enable}",gameObject);
        foreach (var state in _state)
        {
            state.ApplyWithInverse(enable);
        }
    }
}
