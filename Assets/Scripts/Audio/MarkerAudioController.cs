using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarkerAudioController : NetworkBehaviour
{
    [Header("All References")]
    [SerializeField] private AudioClip markerPlacedClipAll;
    [SerializeField, Range(0f, 1f)] private float markerPlacedVolumeAll = 1f;
    [SerializeField] private bool playAsUiAll = false;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    [Rpc(SendTo.ClientsAndHost)]
    public void OnMarkerSondPlayRpc(Vector3 hitPoint)
    {
        if (playAsUiAll)
        {
            gameEffectEvent.Invoke(new GameEffect(markerPlacedClipAll, transform.position, volume:markerPlacedVolumeAll));
        }
        else
        {
            Debug.LogWarning($"[{nameof(MarkerAudioController)}]playAsUiAll is false");
        }
    }
}