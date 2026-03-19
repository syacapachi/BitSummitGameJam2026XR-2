using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NGun))]
public class NGunMarkerAudioObserver : NetworkBehaviour
{
    [Header("All References")]
    [SerializeField] private AudioClip markerPlacedClipAll;
    [SerializeField, Range(0f, 1f)] private float markerPlacedVolumeAll = 1f;
    [SerializeField] private bool playAsUiAll = false;

    private NGun nGunAll;
    private InputAction markerActionLocal;
    private bool isBoundLocal = false;

    private void Awake()
    {
        nGunAll = GetComponent<NGun>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        StartCoroutine(BindMarkerActionLocal());
    }

    public override void OnNetworkDespawn()
    {
        UnbindMarkerActionLocal();
    }

    private IEnumerator BindMarkerActionLocal()
    {
        yield return new WaitUntil(() =>
            ManagerLocator.Instance != null &&
            ManagerLocator.Instance.AllPlayerManager != null &&
            ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer != null &&
            ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput != null
        );

        markerActionLocal = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["Marker"];

        if (markerActionLocal == null)
        {
            Debug.LogError("NGunMarkerAudioObserver: Marker action was not found.");
            yield break;
        }

        markerActionLocal.started += OnMarkerStartedLocal;
        isBoundLocal = true;
    }

    private void UnbindMarkerActionLocal()
    {
        if (!isBoundLocal) return;

        if (markerActionLocal != null)
        {
            markerActionLocal.started -= OnMarkerStartedLocal;
        }

        isBoundLocal = false;
    }

    private void OnMarkerStartedLocal(InputAction.CallbackContext context)
    {
        if (markerPlacedClipAll == null) return;
        if (nGunAll == null) return;
        if (nGunAll.markerPoint == null) return;
        if (nGunAll.weaponSettings == null) return;

        RaycastHit hitAll;
        Vector3 forwardAll = nGunAll.markerPoint.forward;

        bool didHitAll = Physics.Raycast(
            nGunAll.markerPoint.position,
            forwardAll,
            out hitAll,
            nGunAll.weaponSettings.laserDistance
        );

        if (!didHitAll) return;

        if (GameAudioManager.Instance != null)
        {
            if (playAsUiAll)
            {
                GameAudioManager.Instance.PlayUI(markerPlacedClipAll, markerPlacedVolumeAll);
            }
            else
            {
                GameAudioManager.Instance.PlayWorld(markerPlacedClipAll, transform.position, markerPlacedVolumeAll);
            }
        }
        else
        {
            Vector3 playPositionAll = playAsUiAll
                ? (Camera.main != null ? Camera.main.transform.position : transform.position)
                : hitAll.point;

            AudioSource.PlayClipAtPoint(markerPlacedClipAll, playPositionAll, markerPlacedVolumeAll);
        }
    }
}