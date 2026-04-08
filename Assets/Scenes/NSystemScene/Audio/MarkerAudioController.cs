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

    [Rpc(SendTo.NotServer)]
    public void OnMarkerSondPlayRpc(Vector3 hitPoint)
    {
        var audioManager = ManagerLocator.Instance.GameAudioManager;
        if (audioManager != null)
        {
            if (playAsUiAll)
            {
                audioManager.PlayUI(markerPlacedClipAll, markerPlacedVolumeAll);
            }
            else
            {
                audioManager.PlayWorld(markerPlacedClipAll, transform.position, markerPlacedVolumeAll);
            }
        }
        else
        {
            Vector3 playPositionAll = playAsUiAll
                ? (Camera.main != null ? Camera.main.transform.position : transform.position)
                : hitPoint;

            AudioSource.PlayClipAtPoint(markerPlacedClipAll, playPositionAll, markerPlacedVolumeAll);
        }
    }
}