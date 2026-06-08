using System;
using System.Collections.Generic;

public sealed class FlagTable<T>
{
    //基本テーブル
    readonly T[][] table;
    //キャッシュ
    readonly Dictionary<int, T[]> cache = new();
    //0用の空列
    static readonly T[] empty = Array.Empty<T>();
    public FlagTable(T[][] table)
    {
        this.table = table;
        List<T> result = new();
        for (int i = 0; i < table.Length; i++)
        {
            if (table[i] != null)
                result.AddRange(table[i]);
        }
        cache[-1] = result.ToArray(); ;
        cache[0] = empty;
    }

    public void Collect(int flags, List<T> result)
    {
        result.Clear();
        if (!cache.TryGetValue(flags, out var cached))
        {
            cached = Build(flags);
            cache[flags] = cached;
        }
        result.AddRange(cached);
        
    }
    public IReadOnlyList<T> Collect(int flags)
    {
        List<T> result = new();
        Collect(flags, result);
        return result;
    }
    private T[] Build(int flags)
    {
        if (flags == 0)
        {
            return empty;
        }
        List<T> result = new List<T>(table.Length);
        for (int i = 0; i < table.Length; i++)
        {
            var arr = table[i];
            if (arr != null && (flags & (1 << i)) != 0)
            {
                result.AddRange(table[i]);
            }
        }
        return result.ToArray();
    }
}