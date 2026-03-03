using UnityEngine;
using Unity.Netcode;
public class NEnemy : NetworkBehaviour
{
    [SerializeField] EnemySO enemySO;
    private int currentHP;

    public override void OnNetworkSpawn()
    {
        currentHP = enemySO.HP;
    }

    // �e�����������Ƃ��ɌĂ�

    public void TakeDamage()
    {
        Debug.Log("Take damage");
        currentHP -= enemySO.Damage;

        if (currentHP <= 0)
        {
            DieRpc();
        }
    }
    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Server)]
    void DieRpc()
    {
        Debug.Log("Die");
        ManagerLocator.Instance.GameManager.EnemyKilled(enemySO.scoreValue);

        GetComponent<NetworkObject>().Despawn(true);
    }
}