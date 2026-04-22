using UnityEngine;
using Unity.Netcode;
public class PlayerMeshChange : NetworkBehaviour
{
    [SerializeField] GameObject ownerMeshRoot;
    [SerializeField] GameObject nonOwnerMeshRoot;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            ownerMeshRoot.SetActive(true);
            nonOwnerMeshRoot.SetActive(false);
        }
        else
        {
            ownerMeshRoot.SetActive(false);
            nonOwnerMeshRoot.SetActive(true);
        }
    }
}
