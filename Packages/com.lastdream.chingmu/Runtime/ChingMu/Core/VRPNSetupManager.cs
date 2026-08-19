using ChingMU;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRPNSetupManager : MonoBehaviour
{
    public CMPluginAPI.CMPluginType cMPluginType = CMPluginAPI.CMPluginType.Vrpn;
    [HideInInspector]
    public string ServerIP;

    [HideInInspector]
    public static CMPluginCommonInterface CMPlugin;

    void Start()
    {
        CMPlugin = new VrpnImpl();
        CMVrpn.CMUnityEnableTrackLog(false);
        CMPlugin.ServerIp = ServerIP;
        CMPlugin.Port = 3883;
        CMPlugin.StartCmThread();     
    }

    private void OnDestroy()
    {
        CMPlugin.QuitCmThread();
    }
}
