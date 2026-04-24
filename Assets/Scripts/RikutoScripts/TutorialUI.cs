using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Syacapachi.Attribute;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] RectTransform leftLine;
    [SerializeField] RectTransform rightLine;
    [SerializeField] CanvasGroup descGroup;

    [OnInspectorButton("Play")]
    public void Play(string title, string desc)
    {
        StartCoroutine(PlayRoutine(title, desc));
    }

    IEnumerator PlayRoutine(string title, string desc)
    {
        // 初期化
        titleText.text = title;
        descText.text = desc;

        descGroup.alpha = 0f;

        leftLine.localScale = new Vector3(0f, 1f, 1f);
        rightLine.localScale = new Vector3(0f, 1f, 1f);

        // ① タイトル表示
        titleText.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        // ② 横線が開く
        float t = 0f;
        float duration = 0.3f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float s = t / duration;

            leftLine.localScale = new Vector3(s, 1f, 1f);
            rightLine.localScale = new Vector3(s, 1f, 1f);

            yield return null;
        }

        // ③ 説明文フェードイン
        yield return FadeIn(descGroup, 0.3f);
    }

    IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = t / duration;
            yield return null;
        }
        cg.alpha = 1f;
    }
}