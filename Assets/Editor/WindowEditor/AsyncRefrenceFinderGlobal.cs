using Syacapachi.util;
using System.Collections.Generic;
using UnityEditor;
[InitializeOnLoad]
public static class AsyncRefrenceFinderGlobal
{
    //今検索してる奴のリスト(エディター更新時に解除する必要あるから)
    static readonly List<AsyncReferenceFinder> activeFinder = new();
    //エディター更新時に呼ばれる
    static AsyncRefrenceFinderGlobal()
    {
        StopAll(activeFinder);
    }
    public static void Resister(AsyncReferenceFinder finder)
    {
        activeFinder.Add(finder);
    }
    public static void Unresister(AsyncReferenceFinder finder)
    {
        activeFinder.Remove(finder);
    }
    //全部止める
    private static void StopAll(List<AsyncReferenceFinder> activeFinder)
    {
        foreach (var finder in activeFinder)
        {
            finder.StopSearch();
        }
        activeFinder.Clear();
    }
}
