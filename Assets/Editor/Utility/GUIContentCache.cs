namespace Syacapachi.util
{
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
        /// GUILayoutOptionのキャッシュ
        /// GUILayout.Height(height)は内部でnewされる。
        /// </summary>
        private static readonly Dictionary<int, GUILayoutOption> heightCache = new();
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
        public static GUILayoutOption GetHeight(float height)
        {
            int key = Mathf.RoundToInt(height * 1000);
            if (!heightCache.TryGetValue(key, out GUILayoutOption option))
            {
                option = GUILayout.Height(height);
                heightCache.Add(key, option);
            }
            return option;
        }
        /// <summary>
        /// GUIContentのキャッシュを取得
        /// あまり使う効果なし。
        /// 理由:stringを入れたら、使いまわされるObjectが更新されて帰ってくるGUIContnt.Temp();
        /// 要するにこの関数はこれは、メモリ確保することでアクセス高速化
        /// </summary>
        public static GUIContent GetContent(string contentKey)
        {
            if (!contentCache.TryGetValue(contentKey, out var cache))
            {
                cache = new GUIContent(contentKey, contentKey);
                contentCache[contentKey] = cache;
            }
            return cache;
        }
        public static bool TryGetContent(string contentKey, out GUIContent cache)
        {
            if (contentCache.TryGetValue(contentKey, out cache))
            {
                return cache != null;
            }
            return false;
        }
        public static void ResistContent(string key, GUIContent content)
        {
            if (contentCache.ContainsKey(key))
            {
                Debug.LogWarning($"{key} is already resisted");
            }
            contentCache[key] = content;
        }
        public static void ResistContent(string key, string label, Texture image = null, string toolip = "")
        {
            if (contentCache.ContainsKey(key))
            {
                Debug.LogWarning($"{key} is already resisted");
            }
            contentCache[key] = new GUIContent(label, image, toolip);
        }
    }
}