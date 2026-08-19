using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ChingMU;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

public class LiveStreamImpl : CMPluginCommonInterface
{
    string serverAddr;
    int port;
    CMPluginAPI.CMServerType serverType;

    CMPluginAPI.CMPluginType CMPluginCommonInterface.cMpluginType
    {
        get { return CMPluginAPI.CMPluginType.LiveStream; }
    }

    CMPluginAPI.CMServerType CMPluginCommonInterface.cMserverType
    {
        get { return serverType; }
        set
        {
            string[] str = serverAddr.Split('@');
            if (str[0] == "MCAvatar")
            {
                serverType = CMPluginAPI.CMServerType.MCAvatar;
            }
            else
                serverType = CMPluginAPI.CMServerType.MCAvatar;//MCServer;
        }
    }

    string CMPluginCommonInterface.ServerIp 
    {
        get { return serverAddr; }
        set { serverAddr = value; }
    }

    int CMPluginCommonInterface.Port 
    {
        get { return port; }
        set { port = value; }
    }

    bool CMPluginCommonInterface.ConnectCmServer()
    {
        CMPluginAPI.InitConnectInfoForLiveStream(GetIP(), serverAddr);

        return CMPluginAPI.ConnectToServer(2000);
    }

    void CMPluginCommonInterface.StartCmThread() 
    {
        CMPluginAPI.StartClientThread();
    }

    void CMPluginCommonInterface.QuitCmThread()
    {
        CMPluginAPI.QuitClientThread();
    }

    void CMPluginCommonInterface.GetTrackerPose( int BodyId ,out Vector3 wBodyPos,out Quaternion wBodyQuat) 
    {
        wBodyPos = Vector3.zero;
        wBodyQuat = Quaternion.identity;
        CMPluginAPI.tFrame frameData = (CMPluginAPI.tFrame)Marshal.PtrToStructure(CMPluginAPI.GetFrameData(), typeof(CMPluginAPI.tFrame));

        for (int i = 0; i < frameData.bodyNum; i++)
        {
            if (frameData.bodyData[i].id == BodyId)
            {
                wBodyPos = new Vector3(frameData.bodyData[i].pos.x, frameData.bodyData[i].pos.z, frameData.bodyData[i].pos.y) / 1000f;
                wBodyQuat = new Quaternion(frameData.bodyData[i].quat.x,frameData.bodyData[i].quat.z,frameData.bodyData[i].quat.y,-frameData.bodyData[i].quat.w);
                break;
            }
        }
    }

    void CMPluginCommonInterface.GetTrackerPoseByName(string name,int BodyId, out Vector3 wBodyPos, out Quaternion wBodyQuat)
    {
        wBodyPos = Vector3.zero;
        wBodyQuat = Quaternion.identity;
        CMPluginAPI.tFrame frameData = (CMPluginAPI.tFrame)Marshal.PtrToStructure(CMPluginAPI.GetFrameData(), typeof(CMPluginAPI.tFrame));

        for (int i = 0; i < frameData.bodyNum; i++)
        {
            if (frameData.bodyData[i].id == BodyId)
            {
                wBodyPos = new Vector3(frameData.bodyData[i].pos.x, frameData.bodyData[i].pos.z, frameData.bodyData[i].pos.y) / 1000f;
                wBodyQuat = new Quaternion(frameData.bodyData[i].quat.x, frameData.bodyData[i].quat.z, frameData.bodyData[i].quat.y, -frameData.bodyData[i].quat.w);
                break;
            }
        }
    }

    bool CMPluginCommonInterface.GetHumanWithoutRetargetPose(int HuamnId, out Vector3 wPos, Quaternion[] rot) 
    {
        CMPluginAPI.tFrame frameData = (CMPluginAPI.tFrame)Marshal.PtrToStructure(CMPluginAPI.GetFrameData(), typeof(CMPluginAPI.tFrame));
        int isDetected =0;
        wPos = Vector3.zero;
        for (int i =0;i<frameData.humanNum;i++) 
        {
            if (frameData.humanData[i].id== HuamnId) 
            {
                isDetected = frameData.humanData[i].isDetect;
                wPos = new Vector3( frameData.humanData[i].rootPos.x,frameData.humanData[i].rootPos.z,frameData.humanData[i].rootPos.y)/1000f;
                int segementNum = frameData.humanData[i].segementNum;
                for (int j =0;j< segementNum; j++) 
                {
                    if (frameData.humanData[i].isSegmentDetect[j] == 1)
                    {
                        rot[j] = new Quaternion((float)frameData.humanData[i].segmentQuat[j].x, (float)frameData.humanData[i].segmentQuat[j].z, (float)frameData.humanData[i].segmentQuat[j].y, -(float)frameData.humanData[i].segmentQuat[j].w);
                        //segmentIsDetected[j] = true;
                    }
                    else 
                    {
                        rot[j] = Quaternion.identity;
                        //segmentIsDetected[j] = false;
                    }
                }
                break;
            }
        }
        return isDetected == 1 ? true : false;
    }

    public bool GetHumanWithRetargetPose(int HumanID, Vector3[] lPos,  Quaternion[] lRot)
    {
        CMPluginAPI.tFrame  frameData = (CMPluginAPI.tFrame)Marshal.PtrToStructure(CMPluginAPI.GetFrameData(), typeof(CMPluginAPI.tFrame));
        for (int i = 0; i < frameData.humanNum; i++)
        {
            if (frameData.humanData[i].id == HumanID)
            {
                Vector3 hipWpos = new Vector3(frameData.humanData[HumanID].rootPos.x, frameData.humanData[HumanID].rootPos.z, frameData.humanData[HumanID].rootPos.y) / 1000f;
                lPos[1] = hipWpos;
                for (int j = 0; j < 150; j++)
                {
                    lRot[j] = new Quaternion(frameData.humanData[HumanID].segmentQuat[j].x, frameData.humanData[HumanID].segmentQuat[j].z, frameData.humanData[HumanID].segmentQuat[j].y, -frameData.humanData[HumanID].segmentQuat[j].w);

                }
            }
        }
       
        return frameData.humanData[HumanID].isDetect == 1 ? true : false;
    }

    /// <summary>
    /// 获取本机IP
    /// </summary>
    /// <returns>string :ip地址</returns>
    public string GetIP()
    {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adater in adapters)
        {
            if (adater.Supports(NetworkInterfaceComponent.IPv4))
            {
                UnicastIPAddressInformationCollection UniCast = adater.GetIPProperties().UnicastAddresses;
                if (UniCast.Count > 0)
                {
                    foreach (UnicastIPAddressInformation uni in UniCast)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)

                        {
                            Debug.Log(uni.Address.ToString());
                            return uni.Address.ToString();
                        }
                    }
                }
            }
        }
        return null;
    }
}
