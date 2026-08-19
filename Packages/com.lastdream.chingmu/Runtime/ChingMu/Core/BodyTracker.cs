using System.Collections.Generic;
using UnityEngine;

public class BodyTracker : MonoBehaviour
{
    private Vector3 worldPosition;
    private Quaternion worldRotation = Quaternion.identity;
    private CMPluginCommonInterface plugin;
    private string trackerName;

    [Header("刚体ID")]
    public int bodyId;

    [Header("刚体名称")]
    public string bodyName;

    [Header("如果使用配置文件则填写配置文件中刚体对应的Index")]
    public int BodyIDIndex;

    [Header("端口")]
    public string Port = "3883";

    [Header("以刚体名称进行数据接收")]
    public bool isUsingTrackerName;

    [System.Serializable]
    public class JsonData
    {
        public List<int> bodiesID;
    }

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        if (plugin == null)
        {
            return;
        }

        int configuredPort;
        int.TryParse(Port, out configuredPort);
        string address = ChingMuAddress.Build(plugin.ServerIp, configuredPort);
        trackerName = bodyName + "@" + ChingMuAddress.Host(address);

        CMPluginThreadManager manager = FindFirstObjectByType<CMPluginThreadManager>();
        if (manager == null || !manager.isUsingConfig)
        {
            return;
        }

        CMUTrackerPreset<int> preset = Config.Instance.CMTrackPreset;
        if (preset != null && BodyIDIndex >= 0 && BodyIDIndex < preset.Bodies.Count)
        {
            bodyId = preset.Bodies[BodyIDIndex];
        }
        else
        {
            Debug.LogWarning("ChingMu body configuration index is not available.", this);
        }
    }

    private void FixedUpdate()
    {
        if (plugin == null)
        {
            return;
        }

        if (isUsingTrackerName)
        {
            plugin.GetTrackerPoseByName(trackerName, bodyId, out worldPosition, out worldRotation);
        }
        else
        {
            plugin.GetTrackerPose(bodyId, out worldPosition, out worldRotation);
        }

        transform.localPosition = worldPosition;
        transform.localRotation = worldRotation;
    }
}
