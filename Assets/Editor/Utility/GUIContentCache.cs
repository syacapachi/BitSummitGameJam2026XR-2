using System.Collections.Generic;
using UnityEngine;

public static class GUIContentCache
{
    /// <summary>
    /// GUILayoutOptionのキャッシュ
    /// GUILayout.Width(width)は内部でnewされる。
    /// </summary>
    private static readonly Dictionary<int, GUILayoutOption> widthCache = new();
    /// <summary>
    /// GUIContentのキャッシュ
    /// あまり使う効果なし。
    /// これは、メモリ確保でアクセス高速化
    /// </summary>
    private static readonly Dictionary<string, GUIContent> contentCache = new();
    /// <summary>
    /// GUILayoutOptionのキャッシュ
    /// GUILayout.Width(width)は内部でnewされる。
    /// </summary>
    /// <param name="width"></param>
    /// <returns></returns>
    public static GUILayoutOption GetWidth(float width)
    {
        int key = Mathf.RoundToInt(width * 1000);
        if (!widthCache.TryGetValue(key, out GUILayoutOption option))
        {
            option = GUILayout.Width(width);
            widthCache.Add(key, option);
        }
        return option;
    }
    /// <summary>
    /// GUIContentのキャッシュを取得
    /// あまり使う効果なし。
    /// 理由:stringを入れたら、使いまわされるObjectが更新されて帰ってくるGUIContnt.Temp();
    /// 要するにこの関数はこれは、メモリ確保することでアクセス高速化
    /// </summary>
    public static GUIContent GetContent(string content)
    {
        if (!contentCache.TryGetValue(content, out var cache))
        {
            cache = new GUIContent(content, content);
        }
        return cache;
    }
    public static void ResistContent(string key, string label, Texture image = null, string toolip = "")
    {
        if(contentCache.ContainsKey(key))
        {
            Debug.LogWarning($"{key} is already resisted");
        }
        contentCache[key] = new GUIContent(label, image, toolip);
    }
}