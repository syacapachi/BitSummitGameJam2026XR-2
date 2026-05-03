using System.Collections;
using UnityEngine;

public class AlertLightController : MonoBehaviour
{
    [SerializeField] GameObject alertObject;

    [Header("Setting")]
    [SerializeField] float rotateSpeed = 100f;

    float timer = 0f;
    [SerializeField] float interval = 3f;
    [SerializeField] AudioClip buzzerClip;
    [SerializeField] AudioEffectData buzzerEffectData;

    [Header("Subscribe Event")]
    [SerializeField] HPInfoEvent hpInfo;
    [SerializeField] BoolEvent alertRpcEvent;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    bool isActive = false;

    void Awake()
    {
        if (alertObject != null)
            alertObject.SetActive(false);
    }
    private void OnEnable()
    {
        alertRpcEvent.Register(OnAlert);
    }
    private void OnDisable()
    {
        alertRpcEvent.Unregister(OnAlert);
    }
    void OnAlert(bool alert)
    {
        isActive = alert;
        if (isActive)
        {
            ActivateAlert();
        }
        else
        {
            DeactivateAlert();
        }
    }

    void ActivateAlert()
    {
        if (isActive) return;
        isActive = true;

        if (alertObject != null)
            alertObject.SetActive(true);

        StartCoroutine(BuzzerRotateCoroutine());
        StartCoroutine(BuzzerCoroutine());
    }

    void DeactivateAlert()
    {
        if(!isActive) return;
        isActive = false;

        if (alertObject != null)
            alertObject.SetActive(false);
    }
    IEnumerator BuzzerRotateCoroutine()
    {
        while (isActive && alertObject != null)
        {
            alertObject.transform.Rotate(rotateSpeed * Time.deltaTime * Vector3.up);
            yield return null;
        }
    }
    IEnumerator BuzzerCoroutine()
    {
        WaitForSeconds waitInterval = new WaitForSeconds(interval);
        while (isActive)
        {
            gameEffectEvent.Invoke(new GameEffect(buzzerEffectData.ToRuntimeData(), alertObject.transform.position));
            yield return waitInterval;
        }
    }
}