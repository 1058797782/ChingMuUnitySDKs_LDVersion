using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChingMU;

public class VrpnImpl : CMPluginCommonInterface
{
    string serverAddr;
    int port;
    string NoRetargetServerAddr ;
    string RetargetServerAddr;

    CMPluginAPI.CMServerType serverType;

    int CMPluginCommonInterface.Port
    {
        set 
        { 
            port = value;
            NoRetargetServerAddr = serverAddr + ":" + port;
            RetargetServerAddr = serverAddr + ":" + port;
        }
        get { return port; }
    }

    string CMPluginCommonInterface.ServerIp 
    {
        set { serverAddr = value; }
        get { return serverAddr; }
    }
    CMPluginAPI.CMPluginType CMPluginCommonInterface.cMpluginType
    {
        get { return CMPluginAPI.CMPluginType.Vrpn; }
    }
    CMPluginAPI.CMServerType CMPluginCommonInterface.cMserverType
    {
        set
        {
            string[] str = serverAddr.Split('@');
            if (str[0] == "MCAvatar")
            {
                serverType = CMPluginAPI.CMServerType.MCAvatar;
            }else
                serverType = CMPluginAPI.CMServerType.MCServer;
        }
        get { return serverType;}
    }
    bool CMPluginCommonInterface.ConnectCmServer() 
    {
        string ConnectStr = serverAddr + ":" + port;
        return CMPluginAPI.CMPluginConnectServer(ConnectStr);
    }

    void CMPluginCommonInterface.StartCmThread()
    {
        CMPluginAPI.CMUnityStartExtern();
    }

    void CMPluginCommonInterface.QuitCmThread()
    {
        CMPluginAPI.CMUnityQuitExtern();
    }

    void CMPluginCommonInterface.GetTrackerPose(int id, out Vector3 wBodyPos, out Quaternion wBodyQuat)
    {
       
        wBodyPos = new Vector3(
        (float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 0, Time.frameCount) / 1000f,
        (float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 2, Time.frameCount) / 1000f,
        (float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 1, Time.frameCount) / 1000f);
        wBodyQuat = new Quaternion(
        (float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 3, Time.frameCount),
        (float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 5, Time.frameCount),
        (float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 4, Time.frameCount),
       -(float)CMPluginAPI.CMTrackerExtern(NoRetargetServerAddr, id, 6, Time.frameCount));

    }

    void CMPluginCommonInterface.GetTrackerPoseByName(string name, int id, out Vector3 wBodyPos, out Quaternion wBodyQuat)
    {
        wBodyPos = new Vector3(
        (float)CMPluginAPI.CMTrackerExtern(name, id, 0, Time.frameCount) / 1000f,
        (float)CMPluginAPI.CMTrackerExtern(name, id, 2, Time.frameCount) / 1000f,
        (float)CMPluginAPI.CMTrackerExtern(name, id, 1, Time.frameCount) / 1000f);
        wBodyQuat = new Quaternion(
        (float)CMPluginAPI.CMTrackerExtern(name, id, 3, Time.frameCount),
        (float)CMPluginAPI.CMTrackerExtern(name, id, 5, Time.frameCount),
        (float)CMPluginAPI.CMTrackerExtern(name, id, 4, Time.frameCount),
       -(float)CMPluginAPI.CMTrackerExtern(name, id, 6, Time.frameCount));
    }

    bool CMPluginCommonInterface.GetHumanWithoutRetargetPose( int channel, out Vector3 pos, Quaternion[] rot) 
    {
        double[] attitude = new double[1000];
        int[] _isDetected = new int[150];
        bool isDetected = CMPluginAPI.CMHumanExtern(NoRetargetServerAddr, channel, Time.frameCount, attitude, _isDetected);
        pos = new Vector3();
        if (isDetected)
        {
            pos = new Vector3((float)attitude[0], (float)attitude[2], (float)attitude[1]) / 1000f;
            for (int i = 0; i < 150; i++)
            {
                if (_isDetected[i] == 1)
                {
                    rot[i] = new Quaternion((float)attitude[i * 4 + 3], (float)attitude[i * 4 + 5], (float)attitude[i * 4 + 4], -(float)attitude[i * 4 + 6]);
                    //segmentIsDetected[i] = true;
                }
                else
                {
                    rot[i] = Quaternion.identity;
                    //segmentIsDetected[i] = false;
                }
            }
        }
        return isDetected;
    }

    bool CMPluginCommonInterface.GetHumanWithRetargetPose(int HumanID,  Vector3[] lPos,  Quaternion[] lRot)
    {
        int vrpntimecode = 0;
        double[] bonePosition = new double[3 * 150];
        double[] boneAttitude = new double[4 * 150];
        int[] isBoneDetected = new int[150];
        if (lPos == null || lRot == null)
            return false;


       
        //   Debug.Log("   "+ segmentNum);
        string ConnectStr = serverAddr + ":" + port;
        //string ConnectStr = "MCServer@127.0.0.1:3884";
        int segmentNum = 150;
        bool isHumanDetected = CMPluginAPI.CMRetargetHumanExternTC(ConnectStr, HumanID, Time.frameCount, ref vrpntimecode, bonePosition, boneAttitude, isBoneDetected);
        if (isHumanDetected)
        {
            //set rotation
            for (int i = 0; i < segmentNum; ++i)
            {
                if (isBoneDetected[i] == 1)
                {

                    //set pos
                    lPos[i].x = (float)bonePosition[3 * i + 0] / 1000;
                    lPos[i].y = (float)bonePosition[3 * i + 2] / 1000;
                    lPos[i].z = (float)bonePosition[3 * i + 1] / 1000;


                    //Maya Skeleton
                    lRot[i] = new Quaternion((float)boneAttitude[i * 4 + 0], (float)boneAttitude[i * 4 + 2], (float)boneAttitude[i * 4 + 1], -(float)boneAttitude[i * 4 + 3]);
                    //  Debug.Log(pos[i] + "  " + boneRot[i]);
                    // segmentIsDetected[i] = true;
                    if (float.IsNaN(lRot[i].x) || float.IsNaN(lRot[i].y)|| float.IsNaN(lRot[i].z)|| float.IsNaN(lRot[i].w)) 
                    {
                        lRot[i] = Quaternion.identity;
                    }
             
                }
                else
                {
                    lPos[i] = Vector3.zero;
                    lRot[i] = Quaternion.identity;
                    //segmentIsDetected[i] = false;
                }
            }
        }
        return isHumanDetected;
    }


}
