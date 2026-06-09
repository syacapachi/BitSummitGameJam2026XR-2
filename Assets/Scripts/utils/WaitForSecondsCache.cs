using System.Collections.Generic;
using UnityEngine;

public static class WaitForSecondsCache
{
    //キャッシュ化できるやつら
    public static readonly WaitForEndOfFrame EndOfFrame = new();
    public static readonly WaitForFixedUpdate FixedUpdate = new();
    //floatを1000倍値をキーとすることで、int化できる。
    private static readonly Dictionary<int, WaitForSeconds> cache = new();

    public static WaitForSeconds Get(float seconds)
    {
        int key = Mathf.RoundToInt(seconds * 1000);

        if (!cache.TryGetValue(key, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            cache[key] = wait;
        }

        return wait;
    }
}
