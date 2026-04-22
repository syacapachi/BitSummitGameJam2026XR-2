using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHPBarUI : MonoBehaviour
{
    [SerializeField] private Image hpBarImage;
    [SerializeField] private TextMeshProUGUI hpText; // 追加

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

        UpdateHPBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
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
        if (hpBarImage != null)
        {
            hpBarImage.fillAmount = Mathf.Max(0.01f, Mathf.Clamp01(currentHP / maxHP));
        }

        // テキスト更新
        if (hpText != null)
        {
            hpText.text = $"{(int)currentHP} / {(int)maxHP}";
        }

        Debug.Log($"[PlayerHPBarUI] HP updated: {currentHP} / {maxHP}");
    }
}
