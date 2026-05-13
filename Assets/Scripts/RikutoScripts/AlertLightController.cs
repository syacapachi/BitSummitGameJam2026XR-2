using System.Collections;
using UnityEngine;

public class AlertLightController : MonoBehaviour
{
    [SerializeField] GameObject alertObject;

    [Header("Setting")]
    [SerializeField] float rotateSpeed = 100f;

    [SerializeField] float interval = 3f;
    [SerializeField] AudioClip buzzerClip;
    [SerializeField] AudioEffectData buzzerEffectData;

    [Header("Subscribe Event")]
    [SerializeField] HPInfoEvent hpInfo;
    [SerializeField] BoolEvent alertRpcEvent;
    [SerializeField] GameStateEvent gameStateEvent;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;
    [SerializeField] BoolEvent WarningStateEvent;

    bool isActive = false;

    void Awake()
    {
        if (alertObject != null)
            alertObject.SetActive(false);
    }
    private void OnEnable()
    {
        gameStateEvent.Register(GameStateChanged);
        alertRpcEvent.Register(OnAlert);
        WarningStateEvent.Register(OnWarningState);
    }
    private void OnDisable()
    {
        gameStateEvent.Unregister(GameStateChanged);
        alertRpcEvent.Unregister(OnAlert);
        WarningStateEvent.Unregister(OnWarningState);
    }
    void GameStateChanged(GameState newState)
    {
        switch(newState)
        {
            case GameState.Home:
            case GameState.Initializing:
                OnAlert(false);break;
        }
    }
    void OnAlert(bool alert)
    {
        if (alert)
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

    void OnWarningState(bool active)
    {
        if (active)
            ActivateAlert();
        else
            DeactivateAlert();
    }
}