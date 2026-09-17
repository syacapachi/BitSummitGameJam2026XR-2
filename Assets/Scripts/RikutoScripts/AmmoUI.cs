using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour,IProgressUI, ICountDownUI
{
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] Image reloadBar; // ImageのTypeを Filled にする

    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color reloadingColor = new Color(0.6f, 0.6f, 0.6f);

    public void UpdateProgress(float progress)
    {
        reloadBar.fillAmount = progress;
    }
    public void UpdateCount(int remainVal, int maxVal)
    {
        if (remainVal == 0)
        {
            ammoText.text = $"0 / {maxVal}";
            ammoText.color = reloadingColor;
            reloadBar.gameObject.SetActive(true); // 確実に表示
        }
        else
        {
            ammoText.text = $"{remainVal} / {maxVal}";
            ammoText.color = normalColor;
            reloadBar.gameObject.SetActive(false);
        }
    }
}