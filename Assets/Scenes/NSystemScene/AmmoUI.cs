using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    public TextMeshProUGUI ammoText;
    public Image reloadBar; // ImageのTypeを Filled にする
    public NGun gun;

    private Color normalColor = Color.black;
    private Color reloadingColor = new Color(0.6f, 0.6f, 0.6f);
    private float reloadStartTime;

    void Start()
    {
        if (gun == null)
        {
            Debug.LogError("Gun not assigned in AmmoUI!");
            return;
        }

        gun.syncedAmmo.OnValueChanged += (_, __) => UpdateAmmoDisplay();
        gun.isReloading.OnValueChanged += (_, __) => UpdateAmmoDisplay();
        gun.isReloading.OnValueChanged += (_, isReloading) =>
        {
            if (isReloading) reloadStartTime = Time.time; // リロード開始時に記録
        };


        UpdateAmmoDisplay();
    }

    void Update()
    {
        if (gun.isReloading.Value)
        {
            float elapsed = Time.time - reloadStartTime;
            reloadBar.fillAmount = Mathf.Clamp01(elapsed / gun.weaponSettings.reloadTime);
            reloadBar.gameObject.SetActive(true); // 確実に表示
        }
        else
        {
            reloadBar.fillAmount = 0;
            reloadBar.gameObject.SetActive(false);
        }
    }

    void UpdateAmmoDisplay()
    {
        if (gun.isReloading.Value)
        {
            ammoText.text = $"0 / {gun.weaponSettings.maxAmmo}";
            ammoText.color = reloadingColor;
            reloadBar.gameObject.SetActive(true);
        }
        else
        {
            ammoText.text = $"{gun.syncedAmmo.Value} / {gun.weaponSettings.maxAmmo}";
            ammoText.color = normalColor;
            reloadBar.gameObject.SetActive(false);
        }
    }
}