using UnityEngine;
using Unity.Netcode;
public class ChangeWhenConnectionEvent : ChangeWhenEvent
{
    void Start()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
            foreach (var state in _state)
            {
                state.Apply(!networkManager.IsClient);
            }
    }
}