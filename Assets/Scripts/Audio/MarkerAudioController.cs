using Unity.Netcode;
using UnityEngine;

public class MarkerAudioController : NetworkBehaviour
{
    [Header("All References")]
    [SerializeField] private AudioClip markerPlacedClipAll;
    [SerializeField] private GameObject markerFxPrefabAll;
    [SerializeField, Range(0f, 1f)] private float markerPlacedVolumeAll = 1f;
    [SerializeField] private bool playAsUiAll = false;

    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    [Rpc(SendTo.ClientsAndHost)]
    public void OnMarkerSondPlayRpc(Vector3 hitPoint)
    {
        if (markerPlacedClipAll == null)
        {
            Debug.LogWarning($"[{nameof(MarkerAudioController)}] markerPlacedClipAll is null");
            return;
        }

        // 各クライアントで「自分の耳元」から鳴らす
        Vector3 playPosition = GetLocalEarPosition();

        if (playAsUiAll)
        {
            gameEffectEvent.Invoke(
                new GameEffect(
                    markerPlacedClipAll,
                    markerFxPrefabAll,
                    playPosition,
                    volume: markerPlacedVolumeAll
                )
            );
        }
        else
        {
            AudioSource.PlayClipAtPoint(markerPlacedClipAll, playPosition, markerPlacedVolumeAll);
        }
    }

    private Vector3 GetLocalEarPosition()
    {
        // AudioListenerがあれば最優先
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener != null)
        {
            return listener.transform.position;
        }

        // 保険：MainCamera
        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        // 最終保険
        return transform.position;
    }
}