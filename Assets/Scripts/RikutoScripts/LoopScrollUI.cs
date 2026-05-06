using UnityEngine;
using System.Collections;

public class LoopScrollUI : MonoBehaviour
{
    [Header("Scroll Images")]
    [SerializeField] RectTransform imageA;
    [SerializeField] RectTransform imageB;

    [Header("Settings")]
    [SerializeField] float speed = 300f;

    float width;
    bool isRunning = false;
    bool initialized = false;

    IEnumerator Start()
    {
        // UIレイアウト確定待ち（超重要）
        yield return null;
        yield return null;

        Init();
    }

    float yA, yB;

    void Init()
    {
        width = ((RectTransform)transform).rect.width;

        yA = imageA.anchoredPosition.y;
        yB = imageB.anchoredPosition.y;

        imageA.anchoredPosition = new Vector2(0, yA);
        imageB.anchoredPosition = new Vector2(width, yB);

        initialized = true;
    }

    IEnumerator ScrollCoruinte()
    {
        if (!initialized) yield break;
        while (isRunning)
        {
            float move = speed * Time.deltaTime;

            Move(imageA, move);
            Move(imageB, move);

            // ループ
            if (imageA.anchoredPosition.x >= width)
            {
                SetPos(imageA, imageB.anchoredPosition.x - width);
            }

            if (imageB.anchoredPosition.x >= width)
            {
                SetPos(imageB, imageA.anchoredPosition.x - width);
            }
            yield return null;
        }
    }

    // Xだけ動かす
    void Move(RectTransform rect, float move)
    {
        var pos = rect.anchoredPosition;
        pos.x += move;
        rect.anchoredPosition = pos;
    }

    // Yを保持してXだけセット
    void SetPos(RectTransform rect, float x)
    {
        var pos = rect.anchoredPosition;
        pos.x = x;
        rect.anchoredPosition = pos;
    }

    public void StartScroll()
    {
        if (!initialized)
        {
            Debug.LogWarning("[LoopScrollUI] Init前にStartScroll呼ばれた → Init強制");
            Init();
        }

        isRunning = true;
        StartCoroutine(ScrollCoruinte());
        Debug.Log("[LoopScrollUI] スクロール開始");
    }

    public void StopScroll()
    {
        isRunning = false;
        Debug.Log("[LoopScrollUI] スクロール停止");
    }
}