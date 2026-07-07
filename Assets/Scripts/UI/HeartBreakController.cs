using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class HeartBreakController : MonoBehaviour
{
    [Header("Apply Image")]
    [SerializeField] Image image;
    [Header("Sprites")]
    [SerializeField] Sprite normalHeart;
    [SerializeField] Sprite[] Breaksprites;  
    /// <summary>
    /// デフォルトに初期化します。
    /// </summary>
    public void ResetHeart()
    {
        image.sprite = normalHeart;
    }
    /// <summary>
    /// ハートの画像を一定期間で入れ替えます。
    /// </summary>
    /// <param name="time"> 最後の画像になるまでの時間 </param>
    /// <returns></returns>
    public void StartBreak(float time)
    {
        StartCoroutine(BreakProgress(time));
    }
    
    private IEnumerator BreakProgress(float time)
    {
        if(Breaksprites.Length == 0) yield break;
        float separation = time / (Breaksprites.Length - 1);
        // 最初は即時
        image.sprite = Breaksprites[0];
        for (int i = 1; i < Breaksprites.Length; i++)
        {
            float next = Time.time + separation;
            while (Time.time < next)
            {
                yield return null;
            }
            image.sprite = Breaksprites[i];
        }
    }
}
