using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBarUI : MonoBehaviour
{
    [SerializeField] private Image hpBarImage;

    private PlayerHealth playerHealth;

    private void Start()
    {
        StartCoroutine(WaitForPlayerHealth());
    }

    private System.Collections.IEnumerator WaitForPlayerHealth()
    {
        while (playerHealth == null)
        {
            var roots = FindObjectsByType<NetworkPlayerRoot>(FindObjectsSortMode.None);
            foreach (var root in roots)
            {
                if (root.IsOwner)
                {
                    playerHealth = root.playerHealth;
                    break;
                }
            }
            yield return null;
        }

        Debug.Log("[PlayerHPBarUI] PlayerHealth found! Subscribing to HP changes.");

        // 初期表示
        UpdateHPBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);

        // HP変化を監視
        playerHealth.OnHPChanged += UpdateHPBar;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHPChanged -= UpdateHPBar;
        }
    }

    private void UpdateHPBar(float currentHP, float maxHP)
    {
        if (hpBarImage == null) return;

        // ★修正: 最小値0.01を設定してバーが完全に消えないようにする
        hpBarImage.fillAmount = Mathf.Max(0.01f, Mathf.Clamp01(currentHP / maxHP));
        Debug.Log($"[PlayerHPBarUI] HP updated: {currentHP} / {maxHP}");
    }
}
