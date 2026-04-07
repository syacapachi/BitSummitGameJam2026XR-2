using UnityEngine;

public class AlertLightController : MonoBehaviour
{
    [SerializeField] GameObject alertObject;

    [Header("Setting")]
    [SerializeField] int threshold = 2000;
    [SerializeField] float rotateSpeed = 100f;

    bool isActive = false;

    ScoreManager scoreManager;

    void Awake()
    {
        if (alertObject != null)
            alertObject.SetActive(false);
    }

    void Start()
    {
        var locator = ManagerLocator.Instance;
        Debug.Log("Locator: " + locator);

        var gameManager = locator?.AllGameManager;
        Debug.Log("GameManager: " + gameManager);

        scoreManager = gameManager?.ScoreManager;
        Debug.Log("ScoreManager: " + scoreManager);
    }

    void Update()
    {
        if (scoreManager == null) return;


        int score = scoreManager.GetScore();
        Debug.Log("Score: " + score);

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
            alertObject.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
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