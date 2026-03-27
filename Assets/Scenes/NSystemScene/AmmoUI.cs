using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AmmoUI : MonoBehaviour
{
    public TextMeshProUGUI ammoText;
    public Image reloadBar; // ImageのTypeを Filled にする

    private Color normalColor = Color.black;
    private Color reloadingColor = new Color(0.6f, 0.6f, 0.6f);

    public void UpdateReloadBar(float progress)
    {
        reloadBar.fillAmount = progress;
    }
    public void UpdateAmmoDisplay(int remainVal, int maxVal)
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