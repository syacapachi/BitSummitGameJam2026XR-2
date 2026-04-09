using System.Net;
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
        int mask = dhcpInfo.Get<int>("netmask");
        int dns1 = dhcpInfo.Get<int>("dns1");
        int dns2 = dhcpInfo.Get<int>("dns2");
        int gateway = dhcpInfo.Get<int>("gateway");
        int ipAddress = dhcpInfo.Get<int>("ipAddress");
        int leaseDuration = dhcpInfo.Get<int>("leaseDuration");
        int serverAddress = dhcpInfo.Get<int>("serverAddress");
        int prefix = GetPrefix_Android();
        string calcmask = PrefixLengthToSubnetMask(prefix);

        Debug.Log($"calcmask = {calcmask}");

        DebugLogAddress(mask, nameof(mask));
        DebugLogAddress(dns1, nameof(dns1));
        DebugLogAddress(dns2, nameof(dns2));
        DebugLogAddress(gateway, nameof(gateway));
        DebugLogAddress(ipAddress, nameof(ipAddress));
        DebugLogAddress(leaseDuration, nameof(leaseDuration));
        DebugLogAddress(serverAddress, nameof(serverAddress));
        DebugLogAddress(prefix, nameof(prefix));
        Debug.Log($"maskInt = {mask}");
        return $"{mask & 0xFF}.{(mask >> 8) & 0xFF}.{(mask >> 16) & 0xFF}.{(mask >> 24) & 0xFF}";
#else
        return "255.255.255.0";
#endif
    }
    public static int GetPrefix_Android()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

    using var cm = activity.Call<AndroidJavaObject>("getSystemService", "connectivity");
    using var network = cm.Call<AndroidJavaObject>("getActiveNetwork");
    using var props = cm.Call<AndroidJavaObject>("getLinkProperties", network);

    var linkAddresses = props.Call<AndroidJavaObject>("getLinkAddresses");

    int size = linkAddresses.Call<int>("size");

    for (int i = 0; i < size; i++)
    {
        using var addr = linkAddresses.Call<AndroidJavaObject>("get", i);
        int prefix = addr.Call<int>("getPrefixLength");

        return prefix;
    }
#endif
        return 24;
    }
    static void DebugLogAddress(int address,string name)
    {
        Debug.Log($"{name}Int ={address},Address{address & 0xFF}.{(address >> 8) & 0xFF}.{(address >> 16) & 0xFF}.{(address >> 24) & 0xFF}");
    }
    static string PrefixLengthToSubnetMask(int prefixLength)
    {
        //全部1
        uint mask = uint.MaxValue << (32 - prefixLength);
        if (prefixLength == 0) mask = 0; // 特殊ケース

        byte[] bytes = System.BitConverter.GetBytes(mask);
        if (System.BitConverter.IsLittleEndian) System.Array.Reverse(bytes);

        return new IPAddress(bytes).ToString();
    }
}
