using System.Collections.Generic;

public sealed class FlagTable<T>
{
    //基本テーブル
    readonly T[][] table;
    //-1用のすべて取得
    readonly T[] all;

    public FlagTable(T[][] table)
    {
        this.table = table;
        List<T> result = new();
        for (int i = 0; i < table.Length; i++)
        {
            result.AddRange(table[i]);
        }
        all = result.ToArray();
    }

    public void Collect(int flags, List<T> result)
    {
        result.Clear();
        if(flags == -1)
        {
            result.AddRange(all);
            return;
        }
        for (int i = 0; i < table.Length; i++)
        {
            if ((flags & (1 << i)) != 0)
            {
                if (table[i] != null)
                    result.AddRange(table[i]);
            }
        }
    }
    public List<T> Collect(int flags)
    {
        List<T> result = new List<T>();
        Collect(flags, result);
        return result;
    }
}