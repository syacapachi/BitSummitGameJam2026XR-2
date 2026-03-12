using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AmmoUI : MonoBehaviour
{
    public TextMeshProUGUI ammoText;
    public Image reloadBar; // ImageのTypeを Filled にする
    public NGun gun;

    private Color normalColor = Color.black;
    private Color reloadingColor = new Color(0.6f, 0.6f, 0.6f);
    void Start()
    {
        if (gun == null)
        {
            Debug.LogError("Gun not assigned in AmmoUI!");
            return;
        }

        gun.syncedAmmo.OnValueChanged += (_, value) => UpdateAmmoDisplay(value == 0);
        gun.OnReloadingChanged += (_, isReloading) => 
        { 
            UpdateAmmoDisplay(isReloading);
            if (isReloading) StartCoroutine(ReloadCorutune()); // リロード開始時に記録
        };



        UpdateAmmoDisplay(false);
    }

    IEnumerator ReloadCorutune()
    {
        WaitForSeconds wait01 = new WaitForSeconds(0.1f);
        reloadBar.gameObject.SetActive(true); // 確実に表示
        for (float t = 0; t < gun.weaponSettings.reloadTime; t += 0.1f)
        {
            reloadBar.fillAmount = t / gun.weaponSettings.reloadTime;
            yield return wait01;
        }
        reloadBar.fillAmount = 0;
        reloadBar.gameObject.SetActive(false);
    }
    void UpdateAmmoDisplay(bool isReloading)
    {
        if (isReloading)
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