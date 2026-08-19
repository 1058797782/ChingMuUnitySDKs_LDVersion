using System;
using ChingMU;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
public class VRPNSetupManager : MonoBehaviour
{
    public CMPluginAPI.CMPluginType cMPluginType = CMPluginAPI.CMPluginType.Vrpn;

    [HideInInspector]
    public string ServerIP = "MCServer@127.0.0.1";

    [HideInInspector] public static CMPluginCommonInterface CMPlugin;

    private static VRPNSetupManager active;
    private CMPluginCommonInterface plugin;
    private bool ownsNativeThread;

    private void Awake()
    {
        if (active != null && active != this)
        {
            Debug.LogWarning("Only one ChingMu VRPN setup manager can be active.", this);
            enabled = false;
            return;
        }

        active = this;
    }

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        if (plugin != null)
        {
            CMPlugin = plugin;
            return;
        }

        plugin = new VrpnImpl
        {
            ServerIp = string.IsNullOrWhiteSpace(ServerIP) ? "MCServer@127.0.0.1" : ServerIP,
            Port = 3883
        };
        CMPlugin = plugin;

        try
        {
            CMVrpn.CMUnityEnableTrackLog(false);
            plugin.StartCmThread();
            ownsNativeThread = true;
            if (!plugin.ConnectCmServer())
            {
                Debug.LogWarning("ChingMu VRPN setup server connection was not established.", this);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("ChingMu VRPN setup could not start: " + exception.Message, this);
            plugin = null;
            CMPlugin = null;
        }
    }

    private void OnDestroy()
    {
        if (active != this)
        {
            return;
        }

        if (ownsNativeThread && plugin != null)
        {
            try
            {
                plugin.QuitCmThread();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ChingMu VRPN setup could not stop cleanly: " + exception.Message, this);
            }
        }

        if (CMPlugin == plugin)
        {
            CMPlugin = null;
        }
        active = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        active = null;
        CMPlugin = null;
    }
}
