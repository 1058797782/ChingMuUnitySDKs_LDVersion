using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ChingMU;
using UnityEngine;

public class LiveStreamImpl : CMPluginCommonInterface
{
    private readonly ChingMuLiveFrameReader frameReader = new ChingMuLiveFrameReader();

    private string serverAddress = string.Empty;
    private int port;
    private CMPluginAPI.CMServerType serverType = CMPluginAPI.CMServerType.MCAvatar;

    public string ServerIp
    {
        get { return serverAddress; }
        set
        {
            serverAddress = value ?? string.Empty;
            serverType = ChingMuAddress.ServerType(serverAddress);
        }
    }

    public int Port
    {
        get { return port; }
        set { port = value; }
    }

    public CMPluginAPI.CMPluginType cMpluginType
    {
        get { return CMPluginAPI.CMPluginType.LiveStream; }
    }

    public CMPluginAPI.CMServerType cMserverType
    {
        get { return serverType; }
        set { serverType = value; }
    }

    public bool ConnectCmServer()
    {
        string serverHost = ChingMuAddress.Host(serverAddress);
        if (serverHost.Length == 0)
        {
            return false;
        }

        CMPluginAPI.InitConnectInfoForLiveStream(GetIPForServer(serverHost), serverHost);
        frameReader.Invalidate();
        return CMPluginAPI.ConnectToServer(2000);
    }

    public void StartCmThread()
    {
        CMPluginAPI.StartClientThread();
    }

    public void QuitCmThread()
    {
        frameReader.Invalidate();
        CMPluginAPI.QuitClientThread();
    }

    public void GetTrackerPose(int bodyId, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        frameReader.TryGetBodyPose(bodyId, out worldPosition, out worldRotation);
    }

    public void GetTrackerPoseByName(
        string name,
        int bodyId,
        out Vector3 worldPosition,
        out Quaternion worldRotation)
    {
        frameReader.TryGetBodyPose(bodyId, out worldPosition, out worldRotation);
    }

    public bool GetHumanWithoutRetargetPose(int humanId, out Vector3 worldPosition, Quaternion[] rotations)
    {
        return frameReader.TryGetHumanPose(humanId, null, rotations, out worldPosition, false);
    }

    public bool GetHumanWithRetargetPose(int HumanID, Vector3[] lPos, Quaternion[] lRot)
    {
        Vector3 rootPosition;
        return frameReader.TryGetHumanPose(HumanID, lPos, lRot, out rootPosition, true);
    }

    public string GetIP()
    {
        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                !adapter.Supports(NetworkInterfaceComponent.IPv4))
            {
                continue;
            }

            foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address.Address))
                {
                    return address.Address.ToString();
                }
            }
        }

        return IPAddress.Loopback.ToString();
    }

    private string GetIPForServer(string serverHost)
    {
        try
        {
            using (Socket routeProbe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                routeProbe.Connect(serverHost, 1);
                IPEndPoint localEndPoint = routeProbe.LocalEndPoint as IPEndPoint;
                if (localEndPoint != null)
                {
                    return localEndPoint.Address.ToString();
                }
            }
        }
        catch (SocketException)
        {
        }

        return GetIP();
    }
}
