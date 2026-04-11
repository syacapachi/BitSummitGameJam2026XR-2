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
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity == null)
            {
                Debug.LogWarning("AndroidIPv4NetworkInfo: currentActivity is null.");
                return false;
            }

            using var connectivityManager = activity.Call<AndroidJavaObject>("getSystemService", "connectivity");
            using var activeNetwork = connectivityManager?.Call<AndroidJavaObject>("getActiveNetwork");
            using var linkProperties = activeNetwork != null
                ? connectivityManager?.Call<AndroidJavaObject>("getLinkProperties", activeNetwork)
                : null;

            if (TryGetFromLinkProperties(linkProperties, out info))
            {
                return true;
            }

            if (TryGetFromDhcpInfo(activity, out info))
            {
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
#endif

        return false;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool TryGetFromLinkProperties(AndroidJavaObject linkProperties, out AndroidIPv4NetworkInfo info)
    {
        info = null;

        if (linkProperties == null)
        {
            return false;
        }

        using var linkAddresses = linkProperties.Call<AndroidJavaObject>("getLinkAddresses");
        if (linkAddresses == null)
        {
            return false;
        }

        int size = linkAddresses.Call<int>("size");
        for (int i = 0; i < size; i++)
        {
            using var linkAddress = linkAddresses.Call<AndroidJavaObject>("get", i);
            if (linkAddress == null)
            {
                continue;
            }

            using var inetAddress = linkAddress.Call<AndroidJavaObject>("getAddress");
            if (inetAddress == null)
            {
                continue;
            }

            string hostAddress = inetAddress.Call<string>("getHostAddress");
            if (!IPAddress.TryParse(hostAddress, out var ipAddress))
            {
                continue;
            }

            if (ipAddress.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ipAddress))
            {
                continue;
            }

            int prefixLength = linkAddress.Call<int>("getPrefixLength");
            if (prefixLength < 0 || prefixLength > 32)
            {
                continue;
            }

            info = new AndroidIPv4NetworkInfo(ipAddress, prefixLength);
            return true;
        }

        return false;
    }

    private static bool TryGetFromDhcpInfo(AndroidJavaObject activity, out AndroidIPv4NetworkInfo info)
    {
        info = null;

        using var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
        using var dhcpInfo = wifiManager?.Call<AndroidJavaObject>("getDhcpInfo");

        if (dhcpInfo == null)
        {
            return false;
        }

        int ipInt = dhcpInfo.Get<int>("ipAddress");
        int maskInt = dhcpInfo.Get<int>("netmask");

        if (ipInt == 0 || maskInt == 0)
        {
            return false;
        }

        var ipAddress = IntToIPv4(ipInt);
        var subnetMask = IntToIPv4(maskInt);
        int prefixLength = SubnetMaskToPrefixLength(subnetMask);

        info = new AndroidIPv4NetworkInfo(ipAddress, prefixLength);
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
