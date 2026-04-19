using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AvatarFollower : NetworkBehaviour
{
    Transform target;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        FindTarget();

        // ƒV[ƒ“‘JˆÚ‚ÉÄæ“¾
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded ¨ targetÄæ“¾");
        target = null;
        FindTarget();
    }

    void FindTarget()
    {
        var pm = ManagerLocator.Instance?.AllPlayerManager;
        var local = pm?.LocalPlayerRoot;

        if (local != null)
        {
            target = local.PlayerRoot;
            Debug.Log("Target set!");
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}