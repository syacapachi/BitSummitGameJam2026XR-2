using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBarUI : MonoBehaviour
{
    [SerializeField] private Image hpBarImage;
    [SerializeField] private TextMeshProUGUI hpText; // 追加
    [Header("Subscribe Event")]
    [SerializeField] HPInfoEvent HpInfoEvent;

    private void OnEnable()
    {
        HpInfoEvent.Register(UpdateHPBar);
    }
    private void OnDisable()
    {
        HpInfoEvent.Unregister(UpdateHPBar);
    }

    private void UpdateHPBar(HPInfo info)
    {
        if (hpBarImage != null)
        {
            hpBarImage.fillAmount = Mathf.Max(0.01f, Mathf.Clamp01(info.CurrentHP * info.InvMapHP));
        }

        // テキスト更新
        if (hpText != null)
        {
            hpText.text = $"{info.CurrentHP} / {info.MaxHP}";
        }
    }
}
