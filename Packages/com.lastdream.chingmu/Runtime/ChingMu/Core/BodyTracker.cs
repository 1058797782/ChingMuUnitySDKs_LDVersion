using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class BodyTracker : MonoBehaviour
{
    Vector3 wPos = new Vector3();
    Quaternion wQuat = new Quaternion();

    CMPluginCommonInterface CMPlugin;     

    [Header("刚体ID")]
    public int bodyId;

    [Header("刚体名称")]
    public string bodyName;

    [Header("如果使用配置文件则填写配置文件中刚体对应的Index")]
    public int BodyIDIndex;

    [Header("端口")]
    public string Port = "3883";

    string _address;
    string _name;

    string ip;

    [Header("以刚体名称进行数据接收")]
    public bool isUsingTrackerName = false;

    [System.Serializable]
    public class JsonData
    {
        public List<int> bodiesID;
    }

    private void Start()
    {
        if (CMPluginThreadManager.CMPlugin != null) 
        {            
            CMPlugin = CMPluginThreadManager.CMPlugin;
            _address = CMPlugin.ServerIp + ":" + Port;

            CMPluginThreadManager cmManager = FindFirstObjectByType<CMPluginThreadManager>();

            int atIndex = _address.IndexOf('@');
            int colonIndex = _address.IndexOf(':');

            if (atIndex >= 0 && colonIndex > atIndex)
            {
                ip = _address.Substring(atIndex + 1, colonIndex - atIndex - 1);
            }

            _name = bodyName + "@" + ip;

            Debug.Log("IP: " + _name);

            if (cmManager.isUsingConfig)
            {
                string filePath = Path.Combine(Application.streamingAssetsPath, "Config.json");
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    JsonData data = JsonUtility.FromJson<JsonData>(jsonContent);

                    bodyId = data.bodiesID[BodyIDIndex];
                    Debug.Log("Bodies: " + bodyId);
                }
                else
                {
                    Debug.LogError("JSON文件未找到！");
                }
            }
        }
    }
    void FixedUpdate()
    {
        // 获取追踪体位置和旋转信息，第一个参数代表追踪系统的IP，第二个参数代表追踪体ID
        bool IsInit = CMPlugin == null ? false : true;

        if(isUsingTrackerName)
        {
            if (IsInit)
            {
                CMPluginThreadManager.CMPlugin.GetTrackerPoseByName(_name, bodyId, out wPos, out wQuat);
            }
        }else
        {
            if (IsInit)
            {
                CMPluginThreadManager.CMPlugin.GetTrackerPose(bodyId, out wPos, out wQuat);  
            }
        }
       
        transform.localPosition = wPos;
        transform.localRotation = wQuat;
    }
}
