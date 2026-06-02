using Syacapachi.Attribute;
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
    public Color sunColor = Color.white;
    [Header("光源の強さ")]
    public float sunIntensity = 0.5f;
    [Header("太陽の光の減衰率"), Tooltip("1以上の場合、光は跳ね返るたびに弱くなる。1未満の場合、光は跳ね返るたびに強くなる")]
    public float sunMultiplier = 1f;

    [Header("月の説明")]
    [Header("月の色")]
    public Color moonColor = Color.red;
    [Header("月の光の強さ")]
    public float moonIntensity = 0.1f;
    [Header("月の光の減衰率")]
    public float moonMultiplier = 1f;
    [Header("テクスチャの存在感"),Range(0f,1f)]
    public float textureStrength = 1f;
    [Header("テクスチャの回転"),Range(0f,1f)]
    public float textureRotation = 1f;
    private const float onehour = 360f / 24f;
    private static readonly Vector3 loopx = 360f * Vector3.right;

    //0時->±180
    //6時-> -90 or 270
    //12時 -> 0 or 360
    //18時 -> 90
    [SerializeField,ReadOnly] private Vector3 skyRootEular;
    public Vector3 SkyRootEular => skyRootEular;
    public Quaternion SkyRotation => Quaternion.Euler(skyRootEular);

#if UNITY_EDITOR
    private void OnValidate()
    {
        skyRootEular = new Vector3((int)timeOfDay * onehour + 180f, 170, 0);
    }
#endif

    public void UpdateLerpSky(SkyBoxColorSetting fromSky, SkyBoxColorSetting toSky, float t)
    {
        topColor = Color.Lerp(fromSky.topColor, toSky.topColor, t);
        horizonColor = Color.Lerp(fromSky.horizonColor, toSky.horizonColor, t);
        bottomColor = Color.Lerp(fromSky.bottomColor, toSky.bottomColor, t);

        intensity = Mathf.Lerp(fromSky.intensity, toSky.intensity, t);
        exponentBottom = Mathf.Lerp(fromSky.exponentBottom, toSky.exponentBottom, t);
        exponentTop = Mathf.Lerp(fromSky.exponentTop, toSky.exponentTop, t);

        sunColor = Color.Lerp(fromSky.sunColor, toSky.sunColor, t);
        sunIntensity = Mathf.Lerp(fromSky.sunIntensity, toSky.sunIntensity, t);
        sunMultiplier = Mathf.Lerp(fromSky.sunMultiplier, toSky.sunMultiplier, t);

        moonColor = Color.Lerp(fromSky.moonColor, toSky.moonColor, t);
        moonIntensity = Mathf.Lerp(fromSky.moonIntensity, toSky.moonIntensity, t);
        moonMultiplier = Mathf.Lerp(fromSky.moonMultiplier, toSky.moonMultiplier, t);

        textureStrength = Mathf.Lerp(fromSky.textureStrength, toSky.textureStrength, t);

        if (fromSky.textureRotation > toSky.textureRotation)
        {
            float rot = Mathf.Lerp(fromSky.textureRotation, toSky.textureRotation + 1, t);
            textureRotation = rot <= 1 ? rot : rot -1;
        }
        else
        {
            textureRotation = Mathf.Lerp(fromSky.textureRotation, toSky.textureRotation, t);
        }
        if (fromSky.SkyRootEular.x > toSky.SkyRootEular.x)
        {
            Vector3 eular = Vector3.Lerp(fromSky.SkyRootEular, toSky.SkyRootEular + loopx, t);
            skyRootEular = eular;
        }
        else
        {
            skyRootEular = Vector3.Lerp(fromSky.SkyRootEular, toSky.SkyRootEular, t);
        }
    }
    public void CopySky(SkyBoxColorSetting setting)
    {
        timeOfDay = setting.timeOfDay;

        topColor = setting.topColor;
        horizonColor = setting.horizonColor;
        bottomColor = setting.bottomColor;

        intensity = setting.intensity;
        exponentBottom = setting.exponentBottom;
        exponentTop = setting.exponentTop;

        sunColor = setting.sunColor;
        sunIntensity = setting.sunIntensity;
        sunMultiplier = setting.sunMultiplier;

        moonColor = setting.moonColor;
        moonIntensity = setting.moonIntensity;
        moonMultiplier = setting.moonMultiplier;

        textureStrength = setting.textureStrength;
        textureRotation = setting.textureRotation;

        skyRootEular = setting.skyRootEular;
    }
}
/// <summary>
/// 時間設定を行うための列挙体。
/// 時間は24時間表記で、0時はTwentyFourとする。
/// 時間以外入れてもいいよ。
/// </summary>
public enum TimeOfDay
{
    TwentyFour = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Eleven = 11,
    Twelve = 12,
    Thirteen = 13,
    Fourteen = 14,
    Fifteen = 15,
    Sixteen = 16,
    Seventeen = 17,
    Eighteen = 18,
    Nineteen = 19,
    Twenty = 20,
    TwentyOne = 21,
    TwentyTwo = 22,
    TwentyThree = 23
}