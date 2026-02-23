using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHP = 1;       // ‚±‚Ì“G‚ª‰½”­‚Å“|‚ê‚é‚©
    public int scoreValue = 100; // “|‚µ‚½‚Æ‚«‚É“ü‚éƒXƒRƒA

    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    // ’e‚ª“–‚½‚Á‚½‚Æ‚«‚ÉŒÄ‚Ô
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
        // ƒXƒRƒA‰ÁŽZ
        GameManager.Instance.AddScore(scoreValue);
        // GameManager ‚É’Ê’m‚µ‚Ä‘SŒ‚”jƒ{[ƒiƒX”»’è
        GameManager.Instance.EnemyKilled();

        // “G‚ðíœ
        Destroy(gameObject);
    }
}