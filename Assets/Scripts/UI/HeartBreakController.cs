using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class HeartBreakController : MonoBehaviour
{
    [Header("Apply Image")]
    [SerializeField] Image image;
    [Header("Setting")]
    [SerializeField] float maxExpand = 2f;
    [Header("Sprites")]
    [SerializeField] Sprite normalHeart;
    [SerializeField] Sprite[] Breaksprites;  
    /// <summary>
    /// デフォルトに初期化します。
    /// </summary>
    public void ResetHeart()
    {
        image.sprite = normalHeart;
        image.rectTransform.localScale = Vector3.one;
    }
    /// <summary>
    /// ハートの画像を一定期間で入れ替えます。
    /// </summary>
    /// <param name="duration"> 最後の画像になるまでの時間 </param>
    /// <returns></returns>
    public void StartBreak(float duration)
    {
        StartCoroutine(BreakProgress(duration));
    }
    
    private IEnumerator BreakProgress(float duration)
    {
        if (Breaksprites.Length == 0) yield break;
        if (Breaksprites.Length <= 1)
        {
            image.sprite = Breaksprites[0];
            image.rectTransform.localScale = Vector3.one * maxExpand;
            yield break;
        }
        float separation = duration / (Breaksprites.Length - 1);
        Vector3 sepScale = Vector3.one * (maxExpand - 1) / Breaksprites.Length;
        // 最初は即時
        image.sprite = Breaksprites[0];
        //拡大率
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one + sepScale;
        for (int i = 1; i < Breaksprites.Length; i++)
        {
            float elapsed = 0f;
            while (elapsed < separation)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / separation);
                image.rectTransform.localScale = Vector3.Lerp(startScale, targetScale , t);
                yield return null;
            }
            image.sprite = Breaksprites[i];
            startScale = targetScale;
            targetScale = Vector3.one + sepScale * (i+1);
        }
        image.sprite = Breaksprites[^1];
        image.rectTransform.localScale = Vector3.one * maxExpand;
    }
}
