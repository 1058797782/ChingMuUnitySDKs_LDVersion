using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ChingMU;

public  interface CMPluginCommonInterface
{
    string ServerIp 
    {
        get;
        set;
    }

    int Port 
    {
        get;
        set;
    }

    CMPluginAPI.CMPluginType cMpluginType 
    {
        get;
    }

    CMPluginAPI.CMServerType cMserverType
    {
        get;
        set;
    }

    void StartCmThread();

    void QuitCmThread();

    bool ConnectCmServer();

    void GetTrackerPose(int channel, out Vector3 wBodyPos, out Quaternion wBodyQuat);

    void GetTrackerPoseByName(string name,int channel, out Vector3 wBodyPos, out Quaternion wBodyQuat);
    bool GetHumanWithoutRetargetPose(int HumanID, out Vector3 wPos, [In, Out] Quaternion[] lRot);

    bool GetHumanWithRetargetPose(int HumanID, [In, Out] Vector3[] lPos, [In, Out] Quaternion[] lRot);
}
