// using Unity.Netcode;
// using UnityEngine;

// public class MarkerAudioController : NetworkBehaviour
// {
//     [Header("All References")]
//     [SerializeField] private AudioClip markerPlacedClipAll;
//     [SerializeField] private GameObject markerFxPrefabAll;
//     [SerializeField, Range(0f, 1f)] private float markerPlacedVolumeAll = 1f;
//     [SerializeField] private bool playAsUiAll = false;

//     [Header("Publish Event")]
//     [SerializeField] GameEffectEvent gameEffectEvent;

//     [Rpc(SendTo.ClientsAndHost)]
//     public void OnMarkerSondPlayRpc(Vector3 hitPoint)
//     {
//         if (markerPlacedClipAll == null)
//         {
//             Debug.LogWarning($"[{nameof(MarkerAudioController)}] markerPlacedClipAll is null");
//             return;
//         }

//         // 各クライアントで「自分の耳元」から鳴らす
//         Vector3 playPosition = GetLocalEarPosition();

//         if (playAsUiAll)
//         {
//             gameEffectEvent.Invoke(
//                 new GameEffect(
//                     markerPlacedClipAll,
//                     markerFxPrefabAll,
//                     playPosition,
//                     volume: markerPlacedVolumeAll
//                 )
//             );
//         }
//         else
//         {
//             AudioSource.PlayClipAtPoint(markerPlacedClipAll, playPosition, markerPlacedVolumeAll);
//         }
//     }

//     private Vector3 GetLocalEarPosition()
//     {
//         // AudioListenerがあれば最優先
//         AudioListener listener = FindFirstObjectByType<AudioListener>();
//         if (listener != null)
//         {
//             return listener.transform.position;
//         }

//         // 保険：MainCamera
//         if (Camera.main != null)
//         {
//             return Camera.main.transform.position;
//         }

//         // 最終保険
//         return transform.position;
//     }
// }


//水野が追加した。
using Unity.Netcode;
using UnityEngine;

public class MarkerAudioController : NetworkBehaviour
{
    [Header("All References")]
    [SerializeField] private AudioEffectData markerPlacedAudioDataAll;
    [SerializeField] private FxEffectData markerPlacedFxDataAll;
    [SerializeField] private AudioClip markerPlacedClipAll;
    [SerializeField] private GameObject markerFxPrefabAll;
    [SerializeField, Range(0f, 1f)] private float markerPlacedVolumeAll = 1f;

    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    [Rpc(SendTo.ClientsAndHost)]
    public void OnMarkerSondPlayRpc(Vector3 hitPoint)
    {
        // 1. パーティクルはピン位置に1回だけ出す
        if (markerFxPrefabAll != null && gameEffectEvent != null)
        {
            gameEffectEvent.Invoke(
                new GameEffect(
                    markerPlacedAudioDataAll.ToRuntimeData(),
                    markerPlacedFxDataAll.ToRuntimeData(),
                    hitPoint
                ));
            //gameEffectEvent.Invoke(
            //    GameEffect.CreateFxEffect(
            //        markerFxPrefabAll,
            //        hitPoint,
            //        fxLifeTime: 4f
            //    )
            //);
        }

        // 2. 音は耳元で1回だけ鳴らす
        if (markerPlacedClipAll == null)
        {
            Debug.LogWarning($"[{nameof(MarkerAudioController)}] markerPlacedClipAll is null", gameObject);
            return;
        }

        Vector3 playPosition = GetLocalEarPosition();
        AudioSource.PlayClipAtPoint(markerPlacedClipAll, playPosition, markerPlacedVolumeAll);
    }

    private Vector3 GetLocalEarPosition()
    {
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener != null)
        {
            return listener.transform.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return transform.position;
    }
}
//水野以上