using System;
using System.Collections.Generic;

[Serializable]
public class RandomTable
{
    private System.Random rng;
    private readonly float[] values;
    private int index;

    public RandomTable(int seed, int size = 1000)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
        rng = new System.Random(seed);

        values = new float[size];
        for (int i = 0; i < size; i++)
            values[i] = ((float)rng.NextDouble()); //0以上1未満 [0.0f 1.0f)
        index = 0;
    }

    /// <summary>
    /// 乱数を1つ取得(0~1.0f)
    /// </summary>
    /// <returns></returns>
    public float NextFloat()
    {
        float v = values[index++];
        if (index >= values.Length)
        {
            index = 0;
        }
        return v;
    }

    /// <summary>
    /// 浮動小数点型の乱数を所得
    /// </summary>
    /// <param name="minInclusive"></param>
    /// <param name="maxExclusive"></param>
    /// <returns></returns>
    public float Range(float minInclusive, float maxExclusive)
    {
        return minInclusive + (maxExclusive - minInclusive) * NextFloat();
    }

    /// <summary>
    /// 整数型の乱数を取得
    /// </summary>
    /// <param name="minInclusive"></param>
    /// <param name="maxExclusive"></param>
    /// <returns></returns>
    public int RangeInt(int minInclusive, int maxExclusive)
    {
        return minInclusive + (int)(NextFloat() * (maxExclusive - minInclusive));
    }

    /// <summary>
    /// 再生成
    /// </summary>
    /// <param name="seed"></param>
    public void Rebuild(int seed)
    {
        rng = new System.Random(seed);
        for (int i = 0; i < values.Length; i++)
            values[i] = ((float)rng.NextDouble());
        index = 0;
    }
    /// <summary>
    /// リストシャッフル
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RangeInt(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
