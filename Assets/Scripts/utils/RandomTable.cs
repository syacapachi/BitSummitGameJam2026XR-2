using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class RandomTable
{
    private System.Random rng;
    private List<float> values;
    private int index;

    public RandomTable(int seed, int size = 1000)
    {
        rng = new System.Random(seed);
        values = new List<float>(size);
        for (int i = 0; i < size; i++)
            values.Add((float)rng.NextDouble()); // 0.0f �` 1.0f
        index = 0;
    }

    // 乱数を1つ取得(0~1.0f)
    public float NextFloat()
    {
        if (values.Count == 0) return 0f;
        float v = values[index];
        index = (index + 1) % values.Count; // ����\
        return v;
    }

    // 浮動小数点型の乱数を所得
    public float Range(float min, float max)
    {
        return min + (max - min) * NextFloat();
    }

    public int RangeInt(int min, int max)
    {
        return min + Mathf.FloorToInt(NextFloat() * (max - min));
    }

    // 再生成
    public void Rebuild(int seed, int size = -1)
    {
        if (size < 0) size = values.Count;
        rng = new System.Random(seed);
        values.Clear();
        for (int i = 0; i < size; i++)
            values.Add((float)rng.NextDouble());
        index = 0;
    }
    public void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
