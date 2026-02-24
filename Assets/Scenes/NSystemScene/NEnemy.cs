using UnityEngine;

public class NEnemy : MonoBehaviour
{
    public int maxHP = 1;       // ���̓G�������œ|��邩
    public int scoreValue = 100; // �|�����Ƃ��ɓ���X�R�A

    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    // �e�����������Ƃ��ɌĂ�
    public void TakeDamage(int damage = 1)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // �X�R�A���Z
        NGameManager.Instance.AddScore(scoreValue);
        // GameManager �ɒʒm���đS���j�{�[�i�X����
        NGameManager.Instance.EnemyKilled();

        // �G���폜
        Destroy(gameObject);
    }
}