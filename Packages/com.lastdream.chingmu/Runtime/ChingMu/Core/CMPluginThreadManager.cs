using System;
using ChingMU;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class CMPluginThreadManager : MonoBehaviour
{
    public CMPluginAPI.CMPluginType cMPluginType;
    public string ServerIP;
    public int port;
    public bool isUsingConfig;

    public static CMPluginThreadManager Active { get; private set; }
    [HideInInspector] public static CMPluginCommonInterface CMPlugin;
    [HideInInspector] public static bool IsConnected;

    public CMPluginCommonInterface Plugin { get; private set; }
    public bool Connected { get; private set; }

    [Serializable]
    public class JsonData
    {
        public string serverIP;
    }

    private bool ownsNativeThread;

    private void Awake()
    {
        if (Active != null && Active != this)
        {
            Debug.LogWarning("Only one ChingMu thread manager can be active. The duplicate component was disabled.", this);
            enabled = false;
            return;
        }

        Active = this;
        ApplyConfiguration();
        Plugin = CreatePlugin();
        CMPlugin = Plugin;

        if (Plugin == null)
        {
            return;
        }

        Plugin.ServerIp = ServerIP;
        Plugin.Port = port;
        Plugin.cMserverType = ChingMuAddress.ServerType(ServerIP);

        try
        {
            if (cMPluginType == CMPluginAPI.CMPluginType.Vrpn)
            {
                CMVrpn.CMUnityEnableTrackLog(false);
            }

            Plugin.StartCmThread();
            ownsNativeThread = true;
        }
        catch (Exception exception)
        {
            Debug.LogError("ChingMu native thread could not start: " + exception.Message, this);
            Plugin = null;
            CMPlugin = null;
        }
    }

    private void Start()
    {
        if (Plugin == null || !ownsNativeThread)
        {
            return;
        }

        try
        {
            Connected = Plugin.ConnectCmServer();
            IsConnected = Connected;
            if (!Connected)
            {
                Debug.LogWarning("ChingMu server connection was not established.", this);
            }
        }
        catch (Exception exception)
        {
            Connected = false;
            IsConnected = false;
            Debug.LogError("ChingMu server connection failed: " + exception.Message, this);
        }
    }

    private void OnDestroy()
    {
        if (Active != this)
        {
            return;
        }

        if (ownsNativeThread && Plugin != null)
        {
            try
            {
                Plugin.QuitCmThread();
            }
            catch (Exception exception)
            {
                Debug.LogError("ChingMu native thread could not stop cleanly: " + exception.Message, this);
            }
        }

        ownsNativeThread = false;
        Connected = false;
        Plugin = null;
        Active = null;
        CMPlugin = null;
        IsConnected = false;
    }

    private CMPluginCommonInterface CreatePlugin()
    {
        return cMPluginType == CMPluginAPI.CMPluginType.LiveStream
            ? (CMPluginCommonInterface)new LiveStreamImpl()
            : new VrpnImpl();
    }

    private void ApplyConfiguration()
    {
        if (!isUsingConfig)
        {
            return;
        }

        string configuredAddress;
        if (Config.TryReadServerAddress(out configuredAddress))
        {
            string currentAddress = ServerIP;
            if (string.IsNullOrWhiteSpace(currentAddress) &&
                cMPluginType == CMPluginAPI.CMPluginType.Vrpn)
            {
                currentAddress = "MCServer@";
            }

            ServerIP = ChingMuAddress.ApplyConfiguredHost(currentAddress, configuredAddress);
        }
        else
        {
            Debug.LogWarning("ChingMu Config.json was not found or did not contain a server address.", this);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Active = null;
        CMPlugin = null;
        IsConnected = false;
    }
}
