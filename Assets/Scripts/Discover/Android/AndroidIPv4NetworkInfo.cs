using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Android の現在有効なネットワークから IPv4 情報を取得する補助クラス。
/// </summary>
/// <remarks>
/// Android では C# の NetworkInterface API が使いづらいため、
/// Java の ConnectivityManager / LinkProperties / DhcpInfo を呼び出して
/// IPv4、PrefixLength、SubnetMask、BroadcastAddress を求める。
/// </remarks>
public sealed class AndroidIPv4NetworkInfo
{
    public IPAddress IPAddress { get; }
    public int PrefixLength { get; }
    public IPAddress SubnetMask { get; }
    public IPAddress BroadcastAddress { get; }

    private AndroidIPv4NetworkInfo(IPAddress ipAddress, int prefixLength)
    {
        IPAddress = ipAddress;
        PrefixLength = prefixLength;
        SubnetMask = PrefixLengthToSubnetMask(prefixLength);
        BroadcastAddress = CalcBroadcast(ipAddress, SubnetMask);
    }

    /// <summary>
    /// Android の現在の IPv4 ネットワーク情報を取得する。
    /// </summary>
    public static bool TryGet(out AndroidIPv4NetworkInfo info)
    {
        info = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            //JAndroid 内にあるJavaのコードを呼ぶことで、IPAddressを取得する。
            //Get<返り値の型>("名前")がフィールド変数,Call<返り値の型>("名前",args...)がクラスメソッドを呼ぶ
            //new AndroidJavaClass は軽い(staticポインターを参照するだけ)が、AndroidJavaObject(インスタンスの取得)は結構重いのでやりすぎ注意
            //package com.unity3d.player;のUnityPlayerクラスを静的取得
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            //static  var UnityPlayer.currentActivity; にアクセス;(基本はstaticしかアクセスできない) C#ではJavaの型を取得できないので、AndroidJavaObjectでラッパーする。
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            //ここまではテンプレ(これが、Androidの関数を呼び出すための重要なプロパティ)

            if (activity == null)
            {
                Debug.LogWarning("AndroidIPv4NetworkInfo: currentActivity is null.");
                return false;
            }
            //欲しいAndroidクラスを呼ぶ方法(ドキュメントが見つからないのでGBTに従ってる)
            //AndroidJavaObject はインスタンスを作るので、非staticメソッドにもアクセス可能。(ただし重い);
            using var connectivityManager = activity.Call<AndroidJavaObject>("getSystemService", "connectivity");
            using var activeNetwork = connectivityManager?.Call<AndroidJavaObject>("getActiveNetwork");
            using var linkProperties = activeNetwork != null
                ? connectivityManager?.Call<AndroidJavaObject>("getLinkProperties", activeNetwork)
                : null;
            //connectivityManager.getActiveNetwork().getLinkProperties()で、LinkPropaty(接続情報を持つクラスへアクセス)
            //Debug.Log($"AndroidIPv4NetworkInfo: activeNetwork is {(activeNetwork != null ? "available" : "null")}.");
            //Debug.Log($"AndroidIPv4NetworkInfo: linkProperties is {(linkProperties != null ? "available" : "null")}.");

            if (TryGetFromLinkProperties(linkProperties, out info))
            {
                Debug.Log(
                    $"AndroidIPv4NetworkInfo: resolved from LinkProperties. " +
                    $"IP={info.IPAddress}, Prefix={info.PrefixLength}, Mask={info.SubnetMask}, Broadcast={info.BroadcastAddress}");
                return true;
            }

            Debug.Log("AndroidIPv4NetworkInfo: LinkProperties path failed. Falling back to DhcpInfo.");

            if (TryGetFromDhcpInfo(activity, out info))
            {
                Debug.Log(
                    $"AndroidIPv4NetworkInfo: resolved from DhcpInfo. " +
                    $"IP={info.IPAddress}, Prefix={info.PrefixLength}, Mask={info.SubnetMask}, Broadcast={info.BroadcastAddress}");
                return true;
            }

            Debug.LogWarning("AndroidIPv4NetworkInfo: failed to resolve IPv4 network info from both LinkProperties and DhcpInfo.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
#endif

        return false;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// 最近のAndroidはinkPropertiesから情報をとる。
    /// </summary>
    /// <param name="linkProperties"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    private static bool TryGetFromLinkProperties(AndroidJavaObject linkProperties, out AndroidIPv4NetworkInfo info)
    {
        info = null;

        if (linkProperties == null)
        {
            Debug.LogWarning("AndroidIPv4NetworkInfo: linkProperties is null.");
            return false;
        }

        using var linkAddresses = linkProperties.Call<AndroidJavaObject>("getLinkAddresses");
        if (linkAddresses == null)
        {
            Debug.LogWarning("AndroidIPv4NetworkInfo: getLinkAddresses returned null.");
            return false;
        }

        //c#的にすると、
        //foreach(IPAddress address in LinkPropaty.getLinkAddresses())
        //{
        //    if(address is IPV4Address ipv4Address)
        //    {
        //        return new AndroidIPv4NetworkInfo(ipv4Address.ipAddress,ipv4Address.prefixLength);
        //    }
        //}
        int size = linkAddresses.Call<int>("size");
        Debug.Log($"AndroidIPv4NetworkInfo: LinkAddresses count = {size}.");

        for (int i = 0; i < size; i++)
        {
            using var linkAddress = linkAddresses.Call<AndroidJavaObject>("get", i);
            if (linkAddress == null)
            {
                Debug.LogWarning($"AndroidIPv4NetworkInfo: LinkAddress[{i}] is null.");
                continue;
            }

            using var inetAddress = linkAddress.Call<AndroidJavaObject>("getAddress");
            if (inetAddress == null)
            {
                Debug.LogWarning($"AndroidIPv4NetworkInfo: LinkAddress[{i}] address is null.");
                continue;
            }

            string hostAddress = inetAddress.Call<string>("getHostAddress");
            int prefixLengthRaw = linkAddress.Call<int>("getPrefixLength");
            Debug.Log($"AndroidIPv4NetworkInfo: LinkAddress[{i}] host={hostAddress}, prefix={prefixLengthRaw}.");

            if (!IPAddress.TryParse(hostAddress, out var ipAddress))
            {
                Debug.LogWarning($"AndroidIPv4NetworkInfo: LinkAddress[{i}] could not parse host address.");
                continue;
            }

            if (ipAddress.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ipAddress))
            {
                Debug.Log($"AndroidIPv4NetworkInfo: LinkAddress[{i}] skipped. Family={ipAddress.AddressFamily}, Loopback={IPAddress.IsLoopback(ipAddress)}.");
                continue;
            }

            int prefixLength = prefixLengthRaw;
            if (prefixLength < 0 || prefixLength > 32)
            {
                Debug.LogWarning($"AndroidIPv4NetworkInfo: LinkAddress[{i}] has invalid prefix length {prefixLength}.");
                continue;
            }

            info = new AndroidIPv4NetworkInfo(ipAddress, prefixLength);
            Debug.Log($"AndroidIPv4NetworkInfo: LinkAddress[{i}] selected as active IPv4 address.");
            return true;
        }

        Debug.LogWarning("AndroidIPv4NetworkInfo: no IPv4 address found in LinkProperties.");
        return false;
    }
    /// <summary>
    /// 古いAndroidはDhcpInfoからとる。
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    private static bool TryGetFromDhcpInfo(AndroidJavaObject activity, out AndroidIPv4NetworkInfo info)
    {
        info = null;

        using var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
        using var dhcpInfo = wifiManager?.Call<AndroidJavaObject>("getDhcpInfo");

        if (dhcpInfo == null)
        {
            Debug.LogWarning("AndroidIPv4NetworkInfo: getDhcpInfo returned null.");
            return false;
        }

        int ipInt = dhcpInfo.Get<int>("ipAddress");
        int maskInt = dhcpInfo.Get<int>("netmask");
        int gatewayInt = dhcpInfo.Get<int>("gateway");
        int serverAddressInt = dhcpInfo.Get<int>("serverAddress");

        Debug.Log(
            "AndroidIPv4NetworkInfo: DhcpInfo raw values " +
            $"ip={ipInt}, mask={maskInt}, gateway={gatewayInt}, server={serverAddressInt}.");

        if (ipInt == 0 || maskInt == 0)
        {
            Debug.LogWarning("AndroidIPv4NetworkInfo: DhcpInfo ipAddress or netmask is zero.");
            return false;
        }

        var ipAddress = IntToIPv4(ipInt);
        var subnetMask = IntToIPv4(maskInt);
        int prefixLength = SubnetMaskToPrefixLength(subnetMask);

        info = new AndroidIPv4NetworkInfo(ipAddress, prefixLength);
        Debug.Log(
            $"AndroidIPv4NetworkInfo: DhcpInfo parsed IP={ipAddress}, Mask={subnetMask}, Prefix={prefixLength}, Broadcast={info.BroadcastAddress}.");
        return true;
    }
#endif

    /// <summary>
    /// Android の little-endian な int 表現を IPv4 に変換する。
    /// </summary>
    private static IPAddress IntToIPv4(int address)
    {
        byte[] bytes =
        {
            (byte)(address & 0xFF),
            (byte)((address >> 8) & 0xFF),
            (byte)((address >> 16) & 0xFF),
            (byte)((address >> 24) & 0xFF)
        };
        return new IPAddress(bytes);
    }

    /// <summary>
    /// PrefixLength から SubnetMask を生成する。
    /// </summary>
    private static IPAddress PrefixLengthToSubnetMask(int prefixLength)
    {
        uint mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        byte[] bytes = BitConverter.GetBytes(mask);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes);
    }

    /// <summary>
    /// SubnetMask から PrefixLength を計算する。
    /// </summary>
    private static int SubnetMaskToPrefixLength(IPAddress subnetMask)
    {
        int bits = 0;
        foreach (byte octet in subnetMask.GetAddressBytes())
        {
            byte value = octet;
            for (int i = 0; i < 8; i++)
            {
                bits += (value & 0x80) != 0 ? 1 : 0;
                value <<= 1;
            }
        }
        return bits;
    }

    /// <summary>
    /// IPv4 と SubnetMask から BroadcastAddress を求める。
    /// </summary>
    private static IPAddress CalcBroadcast(IPAddress ipAddress, IPAddress subnetMask)
    {
        byte[] ipBytes = ipAddress.GetAddressBytes();
        byte[] maskBytes = subnetMask.GetAddressBytes();
        byte[] broadcastBytes = new byte[4];

        for (int i = 0; i < 4; i++)
        {
            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
        }

        return new IPAddress(broadcastBytes);
    }
}
