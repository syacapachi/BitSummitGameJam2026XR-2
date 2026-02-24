using UnityEngine;
using Unity.Netcode;
public class NEnemy : NetworkBehaviour
{
    public int maxHP = 1;       // ���̓G�������œ|��邩
    public int scoreValue = 100; // �|�����Ƃ��ɓ���X�R�A

    private int currentHP;

    public override void OnNetworkSpawn()
    {
        currentHP = maxHP;
    }

    // �e�����������Ƃ��ɌĂ�

    public void TakeDamage(int damage = 1)
    {
        Debug.Log("Take damage");
        currentHP -= damage;

        if (currentHP <= 0)
        {
            DieRpc();
        }
    }
    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Server)]
    void DieRpc()
    {
         Debug.Log("Die");
        // �X�R�A���Z
        ManagerLocator.Instance.GameManager.AddScore(scoreValue);
        // GameManager �ɒʒm���đS���j�{�[�i�X����
        ManagerLocator.Instance.GameManager.EnemyKilled();

        // �G���폜
        GetComponent<NetworkObject>().Despawn(true);
    }
}