using UnityEngine;

[CreateAssetMenu(fileName = "SkyBoxColorSetting", menuName = "Game/SkyBoxColorSetting")]
public class SkyBoxColorSetting : ScriptableObject
{
    [Header("空の真上の色")]
    public Color topColor;
    [Header("空の基本色")]
    public Color horizonColor;
    [Header("空の地平線の色")]
    public Color bottomColor;
    [Header("空の明るさ")]
    public float intensity;
    [Header("空の真上の色の浸食度")]
    public float exponentTop;
    [Header("空の地平線の色の浸食度")]
    public float exponentBottom;
}
