using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public sealed class WifiIPV4Info
{
    public enum PrivateIPv4Range
    {
        Any,
        Ethernet,
        Wifi
    }
    public IPAddress IPAddress { get; }
    public IPAddress SubnetMask { get; }
    public IPAddress BroadcastAddress { get; }
    private WifiIPV4Info(IPAddress ip, IPAddress mask)
    {
        IPAddress = ip;
        SubnetMask = mask;
        BroadcastAddress = CalcBroadcast(ip,mask);
    }
    public override string ToString()
    {
        return $"IPAddress = {IPAddress}, SubNetMask = {SubnetMask}, BroadCastAddress = {BroadcastAddress}";
    }
    /// <summary>
    /// Retrieves a list of IPv4 address information for all active Wi-Fi network interfaces on the local machine.
    /// </summary>
    /// <remarks>Only public IPv4 addresses associated with Wi-Fi interfaces are included. Private IPv4
    /// addresses and non-IPv4 addresses are excluded from the results.</remarks>
    /// <returns>A read-only list of <see cref="WifiIPV4Info"/> objects, each containing IPv4 address details for a Wi-Fi
    /// interface. The list is empty if no suitable Wi-Fi interfaces are found.</returns>
    public static IReadOnlyList<WifiIPV4Info> Create(PrivateIPv4Range type = PrivateIPv4Range.Any)
    {
        List<WifiIPV4Info> list = new List<WifiIPV4Info>();

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android では Java 側の LinkProperties / DhcpInfo から
        // IPv4 情報を取得して BroadcastAddress を組み立てる。
        if (AndroidIPv4NetworkInfo.TryGet(out var androidInfo))
        {
            Debug.Log(
                $"WifiIPV4Info: Android network info resolved. " +
                $"IP={androidInfo.IPAddress}, Mask={androidInfo.SubnetMask}, Broadcast={androidInfo.BroadcastAddress}, Prefix={androidInfo.PrefixLength}");
            list.Add(new WifiIPV4Info(
                androidInfo.IPAddress,
                androidInfo.SubnetMask,
                androidInfo.BroadcastAddress
            ));
        }
        else
        {
            Debug.LogWarning("Android で IPv4 の BroadcastAddress を取得できませんでした。");
        }
#else

        // ネットワークインターフェース一覧から Wi-Fi の IPv4 アドレスを取得
        //ここは、Android だと検出できない
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            //検索ネットワーク
            if(!IsValidInterface(nic,type))
                continue;

            var ipProps = nic.GetIPProperties();

            //UnicastAddresss 1対1通信を行うIPAddressを取得
            foreach (var ua in ipProps.UnicastAddresses)
            {
                // IPv4以外を除外
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                list.Add(new WifiIPV4Info(
                        ua.Address,
                        ua.IPv4Mask
                    )
                );

            }
        }
#endif
        return list;
    }
    private static bool IsValidInterface(NetworkInterface nic, PrivateIPv4Range type)
    {
        // 動作していないものを除外
        if (nic.OperationalStatus != OperationalStatus.Up)
            return false;
        

        // 仮想NIC除外（WSL / Hyper-V / VMware / etc）
        string name = nic.Name.ToLowerInvariant();
        string desc = nic.Description.ToLowerInvariant();

        if (name.Contains("virtual")
            || name.Contains("veth")
            || desc.Contains("hyper-v")
            || desc.Contains("virtual"))
            return false;
        return type switch
        {
            PrivateIPv4Range.Wifi => nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211,
            PrivateIPv4Range.Ethernet => nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet,
            PrivateIPv4Range.Any => nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 
                                    || nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet,
            _ => false
        };
    }

    /// <summary>
    /// Ipアドレスとサブネットから Broadcast を計算
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="mask"></param>
    /// <returns></returns>
    private static IPAddress CalcBroadcast(IPAddress ip, IPAddress mask)
    {
        byte[] ipBytes = ip.GetAddressBytes();
        byte[] maskBytes = mask.GetAddressBytes();
        byte[] broadcast = new byte[4];

        for (int i = 0; i < 4; i++)
            broadcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

        return new IPAddress(broadcast);
    }

}
