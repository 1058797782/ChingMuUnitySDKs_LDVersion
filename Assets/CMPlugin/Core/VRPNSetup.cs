using ChingMU;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TransformData
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public TransformData(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

public class VRPNSetup : MonoBehaviour
{
    public delegate void LogHandler(string message);
    public static event LogHandler OnLog;

    private Dictionary<int, TransformData> trackerTransforms = new Dictionary<int, TransformData>();

    CMPluginCommonInterface CMPlugin;    

    public string Port = "3883";

    string _address;

    int index = 0;

    Vector3 pos;
    Quaternion quat;
    bool isLoopExecuted = false;

    private void Start()
    {
        if (VRPNSetupManager.CMPlugin != null)
        {
            CMPlugin = VRPNSetupManager.CMPlugin;
            _address = CMPlugin.ServerIp + ":" + Port;           
        }
    }

    void FixedUpdate()
    {
        if (isLoopExecuted == false)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 pos;
                Quaternion quat;

                CMPlugin.GetTrackerPose(i, out pos, out quat);
                TransformData data = new TransformData(pos, quat);

                if (pos != new Vector3(0, 0, 0) && quat != new Quaternion(0, 0, 0, 0))
                {
                    trackerTransforms.Add(i, data);
                }
            }

            isLoopExecuted = true;
        }

        if (isLoopExecuted)
        {
            bool IsInit = CMPlugin == null ? false : true;

            if (IsInit)
            {
                for (int i = 0; i < trackerTransforms.Count; i++)
                {
                    CMPlugin.GetTrackerPose(GetKey(i), out pos, out quat);
                    string logMessage = $"BodyCount = [{trackerTransforms.Count}]  |  BodySensor = [{GetKey(i)}]  |  Info : [Position :{pos}]  |  [Rotation :{quat}]";
                    OnLog?.Invoke(logMessage);
                    Debug.Log("BodyCount = [" + trackerTransforms.Count + "]  |  " + " BodySensor = [" + GetKey(i) + "]  |  Info :" + " [Position :" + pos + "]  |  " + "[Rotation :" + quat + "]");
                }
            }
        }
    }
    int GetKey(int Num)
    {
        if (Num < 0 || Num >= trackerTransforms.Count)
        {
            return default;
        }

        int currentIndex = 0;
        foreach (int key in trackerTransforms.Keys)
        {
            if (currentIndex == Num)
            {
                return key;
            }
            currentIndex++;
        }
        return default;
    }
}