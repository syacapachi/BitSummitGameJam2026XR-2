using UnityEngine;
using TMPro;
using System.Collections;

public class PhaseUI : MonoBehaviour
{
    public GameObject phaseBoard;
    public TextMeshProUGUI phaseText;
    private NGameManager nGameManager;

    IEnumerator Start()
    {
        while (ManagerLocator.Instance.AllGameManager == null)
        {
            yield return null;
        }

        nGameManager = ManagerLocator.Instance.AllGameManager;

        Debug.Log("GameManager取得成功");

        InitializeUI();
    }

    void InitializeUI()
    {
        phaseText.gameObject.SetActive(false);
        OnPhaseFinishingChanged(false, nGameManager.phaseFinishing.Value);
        OnPhaseChanged(-1, nGameManager.syncedPhaseIndex.Value);
        OnCountdownChanged(0, nGameManager.countdownValue.Value);

        nGameManager.syncedPhaseIndex.OnValueChanged += OnPhaseChanged;
        nGameManager.OnGameEnd += OnGameFinishedChanged;
        nGameManager.countdownValue.OnValueChanged += OnCountdownChanged;
        nGameManager.allEnemyDeadEvent.OnValueChanged += OnAllEnemyDead;
        //nGameManager.phaseFinishing.OnValueChanged += OnPhaseFinishingChanged;

    }

    void OnPhaseChanged(int oldValue, int newValue)
    {
        Debug.Log($"Phase Changed: {oldValue} -> {newValue}");
        var manager = nGameManager;
        if (manager == null) return;

        if (newValue >= 0 && newValue < manager.phases.Length)
        {
            string text = manager.phases[newValue].phaseDisplayName;
            Show(text);
        }
    }

    void Show(string text)
    {
        Debug.Log($"Show Phase: {text}");
        StartCoroutine(ShowSequence(text));
    }

    void Hide()
    {
        phaseText.gameObject.SetActive(false);
    }
    /*
    void ShowScore()
    {
        int score = nGameManager.GetScore();

        phaseText.text = $"Score : {score} point";
        phaseText.gameObject.SetActive(true);

        CancelInvoke(nameof(Hide));
    }
    */

    public void OnGameFinishedChanged()
    {
        StopAllCoroutines(); // 他の表示を止める

        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 0f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", 0f);

        phaseText.fontSize = 150;
        phaseText.text = "FINISH!";
        phaseText.gameObject.SetActive(true);

        StartCoroutine(PopAnimation());

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 2f);
    }

    void OnCountdownChanged(int oldValue, int newValue)
    {
        if (newValue > 0)
        {
            phaseText.text = newValue.ToString();
            phaseText.gameObject.SetActive(true);

            StartCoroutine(PopAnimation());

            CancelInvoke(nameof(Hide));

            float hideTime = 1f;

            // 最終フェーズ終了カウントダウンなら長くする
            if (nGameManager.syncedPhaseIndex.Value == nGameManager.phases.Length - 1)
            {
                hideTime = 2.3f;
            }

            Invoke(nameof(Hide), hideTime);
        }
    }

    public void ShowPhaseFinish()
    {
        int score = nGameManager.GetScore();
        int phase = nGameManager.syncedPhaseIndex.Value;

        phaseText.text = $"Phase {phase + 1} FINISH!\nScore: {score} point";
        phaseText.gameObject.SetActive(true);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 3f); // 3�b���ɏ���
    }
    void OnPhaseFinishingChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowPhaseFinish(); // �t���O��true�ɂȂ������Ă�
        }
    }

    IEnumerator PopAnimation()
    {
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 1f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", -1f);
        Vector3 normal = Vector3.one;
        Vector3 big = Vector3.one * 1.6f;

        phaseText.transform.localScale = big;

        float t = 0f;
        float duration = 0.05f;

        while (t < duration)
        {
            t += Time.deltaTime;
            phaseText.transform.localScale =
                Vector3.Lerp(big, normal, t / duration);
            yield return null;
        }

        phaseText.transform.localScale = normal;
    }

    IEnumerator ShowSequence(string text)
    {
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 0f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", 0f);
        // text表示
        phaseText.fontSize = 120;   // 大きくする
        phaseText.text = text;
        phaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);

        // START表示
        phaseText.fontSize = 150;   // さらに大きく
        phaseText.text = "START!!";
        StartCoroutine(PopAnimation());

        yield return new WaitForSeconds(1.5f);

        Hide();
    }

    IEnumerator FinishSequence()
    {
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 1f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", -1f);

        for (int i = 3; i >= 1; i--)
        {
            phaseText.fontSize = 140;
            phaseText.text = i.ToString();
            phaseText.gameObject.SetActive(true);

            StartCoroutine(PopAnimation());

            yield return new WaitForSeconds(1f);
        }

        phaseText.fontMaterial.SetFloat("_UnderlayOffsetX", 0f);
        phaseText.fontMaterial.SetFloat("_UnderlayOffsetY", 0f);

        phaseText.fontSize = 160;
        phaseText.text = "FINISH!";

        StartCoroutine(PopAnimation());

        yield return new WaitForSeconds(2f);

        Hide();
    }

    void OnAllEnemyDead(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        StopAllCoroutines();
        CancelInvoke(nameof(Hide));

        StartCoroutine(AllEnemyDeadSequence());
    }

    IEnumerator AllEnemyDeadSequence()
    {
        int bonus = nGameManager.lastClearBonus.Value;

        // ① ALL ENEMY DEAD!（1秒）
        phaseText.fontSize = 100;
        phaseText.text = "ALL ENEMY DEAD!";
        phaseText.gameObject.SetActive(true);

        StartCoroutine(PopAnimation());

        yield return new WaitForSeconds(1f);

        phaseText.text = "CLEAR BONUS";

        StartCoroutine(PopAnimation());

        yield return new WaitForSeconds(1f);


        // ② SCORE表示（1秒）
        phaseText.text = $"SCORE: +{bonus}";

        StartCoroutine(PopAnimation());

        yield return new WaitForSeconds(1f);

        Hide();
    }
}