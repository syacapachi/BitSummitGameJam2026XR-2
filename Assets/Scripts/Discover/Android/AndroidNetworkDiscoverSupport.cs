using UnityEngine;

public class AndroidNetworkDiscoverSupport : MonoBehaviour
{
    public static string GetAndroidIP()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    //JAndroid 内にあるJavaのコードを呼ぶことで、IPAddressを取得
    //new AndroidJavaClass は軽い(スタティックポインターを参照するだけ)が、AndroidJavaObject(インスタンスの取得)は結構重いのでやりすぎ注意
    //package com.unity3d.player;のUnityPlayerクラスを静的取得
    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    //static  var UnityPlayer.currentActivity; にアクセス;(基本はstaticしかアクセスできない) C#ではJavaの型を取得できないので、AndroidJavaObjectでラッパーする。
    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    //Call<返り値の型>("関数名",... args)でメソッドにアクセス
    //AndroidJavaObject はインスタンスを作るので、非staticメソッドにもアクセス可能。;
    using var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
    using var wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");

    //プリミティブ型はとれる。外部パッケージを作る場合は、プリミティブ型にしよう
    int ipInt = wifiInfo.Call<int>("getIpAddress");

    return $"{ipInt & 0xFF}.{(ipInt >> 8) & 0xFF}.{(ipInt >> 16) & 0xFF}.{(ipInt >> 24) & 0xFF}";
#else
        return "127.0.0.1";
#endif
    }
    public static string GetMask()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        //Android 内にあるJavaのコードを呼ぶことで、IPAddressを取得
        //package com.unity3d.player;のUnityPlayerクラスを静的取得
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        //static  var UnityPlayer.currentActivity; にアクセス;(基本はstaticしかアクセスできない) C#ではJavaの型を取得できないので、AndroidJavaObjectでラッパーする。
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        //AndroidJavaObject はインスタンスを作るので、非staticメンバ・メソッドにもアクセス可能。Call<返り値の方>("関数名",... args);
        using var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
        using var dhcpInfo = wifiManager.Call<AndroidJavaObject>("getDhcpInfo");

        //フィールドはGet<>
        int maskInt = dhcpInfo.Get<int>("netmask");
        Debug.Log($"maskInt = {maskInt}");
        return $"{maskInt & 0xFF}.{(maskInt >> 8) & 0xFF}.{(maskInt >> 16) & 0xFF}.{(maskInt >> 24) & 0xFF}";
#else
        return "255.255.255.0";
#endif
    }
}
