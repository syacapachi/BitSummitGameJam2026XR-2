using UnityEngine;

public class AndroidNetworkDiscoverSupport : MonoBehaviour
{
    public static string GetAndroidIP()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    //JAndroid 内にあるJavaのコードを呼ぶことで、IPAddressを取得
    //package com.unity3d.player;のUnityPlayer"クラス
    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    //static AndroidJavaObject UnityPlayer.currentActivity; にアクセス;(基本はstaticしかアクセスできない)
    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    //AndroidJavaObject がインスタンスを作るので、非staticメンバ・メソッドにもアクセス可能。Call<返り値の方>("関数名",... args);
    using var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
    using var wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");

    int ipInt = wifiInfo.Call<int>("getIpAddress");

    return $"{ipInt & 0xFF}.{(ipInt >> 8) & 0xFF}.{(ipInt >> 16) & 0xFF}.{(ipInt >> 24) & 0xFF}";
#else
        return "127.0.0.1";
#endif
    }
    public static string GetMask(string ip)
    {
        return "255.255.255.0";
    }
    public static string GetBroadcast(string ip)
    {
        var parts = ip.Split('.');
        return $"{parts[0]}.{parts[1]}.{parts[2]}.255";
    }
}
