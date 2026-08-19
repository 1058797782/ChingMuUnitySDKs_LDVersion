using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChingMU;
using System.IO;

public class CMPluginThreadManager : MonoBehaviour
{
    public CMPluginAPI.CMPluginType cMPluginType;
    public string ServerIP;
    public int port;

    [HideInInspector]
    public static CMPluginCommonInterface CMPlugin;

    [HideInInspector]
    public static bool IsConnected;

    public bool isUsingConfig;

    [System.Serializable]
    public class JsonData
    {
        public string serverIP;
    }

    private void Awake()
    {
        if (cMPluginType == CMPluginAPI.CMPluginType.LiveStream)
        {
            CMPlugin = new LiveStreamImpl();
            CMPlugin.ServerIp = ServerIP;
            CMPlugin.Port = port;
            CMPlugin.cMserverType = CMPluginAPI.CMServerType.MCAvatar;
            CMPlugin.StartCmThread();
        }
        else if (cMPluginType == CMPluginAPI.CMPluginType.Vrpn)
        {
            CMPlugin = new VrpnImpl();
            CMVrpn.CMUnityEnableTrackLog(false);
            CMPlugin.ServerIp = ServerIP;
            CMPlugin.Port = port;
            CMPlugin.StartCmThread();
        }
        else
        {
            CMPlugin = null;
        }
    }
    void Start()
    {
        IsConnected = CMPlugin.ConnectCmServer();

        if(isUsingConfig)
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "Config.json");
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                JsonData data = JsonUtility.FromJson<JsonData>(jsonContent);
                
                ServerIP = data.serverIP;
                Debug.Log("ServerIP: " + data.serverIP);
            }
            else
            {
                Debug.LogError("JSON文件未找到！");
            }
        }
    }

    void Update()
    {
        
    }
    private void OnDestroy()
    {
        CMPlugin.QuitCmThread();
    }
}
