using System.Collections;
using UnityEngine;

public class AlertLightController : MonoBehaviour
{
    [SerializeField] GameObject alertObject;

    [Header("Setting")]
    [SerializeField] int threshold = 2000;
    [SerializeField] float rotateSpeed = 100f;

    float timer = 0f;
    [SerializeField] float interval = 3f;
    [SerializeField] AudioClip buzzerClip;

    [Header("Subscribe Event")]
    [SerializeField] HPInfoEvent hpInfo;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    bool isActive = false;

    ScoreManager scoreManager;

    void Awake()
    {
        if (alertObject != null)
            alertObject.SetActive(false);
    }

    IEnumerator Start()
    {
        while (true)
        {
            var locator = ManagerLocator.Instance;

            if (locator != null &&
                locator.AllGameManager != null &&
                locator.AllGameManager.ScoreManager != null)
            {
                scoreManager = locator.AllGameManager.ScoreManager;
                Debug.Log("ScoreManager取得成功: " + scoreManager);
                break;
            }

            yield return null;
        }
    }

    void Update()
    {
        if(scoreManager == null) return;
        int score = scoreManager.GetScore();

        if (score <= threshold)
        {
            if (!isActive) ActivateAlert();
        }
        else
        {
            if (isActive) DeactivateAlert();
        }

        if (isActive && alertObject != null)
        {
            alertObject.transform.Rotate(rotateSpeed * Time.deltaTime * Vector3.up);
            timer += Time.deltaTime;

            if (timer >= interval)
            {
                gameEffectEvent.Invoke(new GameEffect(buzzerClip, null, alertObject.transform.position));

                timer = 0f;
            }
        }
    }

    void ActivateAlert()
    {
        isActive = true;

        if (alertObject != null)
            alertObject.SetActive(true);
    }

    void DeactivateAlert()
    {
        isActive = false;

        if (alertObject != null)
            alertObject.SetActive(false);
    }
}