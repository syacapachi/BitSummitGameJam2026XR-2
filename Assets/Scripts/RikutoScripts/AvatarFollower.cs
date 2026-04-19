using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class AvatarFollower : NetworkBehaviour
{
    Transform target;

    public override void OnNetworkSpawn()
    {

        if (!IsOwner) return;

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
            var local = ManagerLocator.Instance?.AllPlayerManager?.LocalPlayerRoot;
            if (local != null)
            {
                target = local.PlayerRoot;
                Debug.Log("Target re-acquired");
            }
            else
            {
                return;
            }
        }
        
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}