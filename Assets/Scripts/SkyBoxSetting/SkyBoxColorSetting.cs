using UnityEngine;

[CreateAssetMenu(fileName = "SkyBoxColorSetting", menuName = "Game/SkyBoxColorSetting")]
public class SkyBoxColorSetting : ScriptableObject
{
    public TimeOfDay timeOfDay;
    [Header("空の色の設定")]
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
    [Header("太陽の設定")]
    [Header("太陽の色")]
    public Color lightColor = Color.white;
    [Header("太陽の角度")]
    public Vector3 lightRotation;
    [Header("光源の強さ")]
    public float lightIntensity = 0.5f;
    [Header("太陽の光の減衰率"),Tooltip("1以上の場合、光は跳ね返るたびに弱くなる。1未満の場合、光は跳ね返るたびに強くなる")]
    public float indirectMultiplier = 1f;

}
/// <summary>
/// 時間設定を行うための列挙体。
/// 時間は24時間表記で、0時はTwentyFourとする。
/// 時間以外入れてもいいよ。
/// </summary>
public enum TimeOfDay
{
    TwentyFour,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Eleven,
    Twelve,
    Thirteen,
    Fourteen,
    Fifteen,
    Sixteen,
    Seventeen,
    Eighteen,
    Nineteen,
    Twenty,
    TwentyOne,
    TwentyTwo,
    TwentyThree,

}