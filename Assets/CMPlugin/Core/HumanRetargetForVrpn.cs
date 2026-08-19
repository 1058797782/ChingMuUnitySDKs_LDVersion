using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using ChingMU;
using UnityEngine;



public class HumanRetargetForVrpn : MonoBehaviour
{
    static string serverType;

    Vector3[] JointLocalPos = new Vector3[150]; //150；
    Quaternion[] JointLocalRot = new Quaternion[150];//150
    bool[] isBoneDetected = new bool[150];//150
    List<Transform> HuamnJointTrans;

    CMPluginAPI.VrpnHierarchy CurCharacterHierResult;
    CMPluginAPI.VrpnHierarchy CurCharacterHier;
    CMPluginAPI.UpdateHierarchyCallback GetCurHierCallback;
    IntPtr callback_args;

    Dictionary<string, Transform> UnityCharAllTransNodeAndNameMap;

    List<Transform> CharAllTransNode = new List<Transform>();

    CMPluginCommonInterface CMPlugin;

    [Header("ChingMUTrackerSeting")]
    [Tooltip("ID is Tracker Client manger list Order index")]
    public int ObjectID_InCMTrackSence = 0;
    static string ServerAddr = string.Empty;
    bool IsRegisterCallBack_Finished = false;

    void Start()
    {
        if (CMPluginThreadManager.CMPlugin == null)
        {
            return;
        }

        CMPlugin = CMPluginThreadManager.CMPlugin;
        ServerAddr = CMPluginThreadManager.CMPlugin.ServerIp;

        GetCMserverType();

        if (serverType == "MCServer")
        {
            ServerAddr = ServerAddr + ":" + CMPluginThreadManager.CMPlugin.Port;
        }

        if (serverType == "MCAvatar")
        {
            ServerAddr = ServerAddr + ":" + CMPluginThreadManager.CMPlugin.Port;
        }

        HuamnJointTrans = new List<Transform>();
        GetRetargetDataMapTransHierarchy(transform);
        UnityCharAllTransNodeAndNameMap = new Dictionary<string, Transform>();
        foreach (Transform var in HuamnJointTrans)
        {
            UnityCharAllTransNodeAndNameMap.Add(var.gameObject.name, var);
        }

        for(int i=0;i<150;i++)
        {
            CharAllTransNode.Add(null);
        }

        CurCharacterHier = new CMPluginAPI.VrpnHierarchy();
        CurCharacterHierResult = new CMPluginAPI.VrpnHierarchy();

        GetCurHierCallback = GetClientThisHumanHierarchy;       

        StartCoroutine(AsyRegisterCallBack_GetVrpnDataOrder());
    }

    bool RegisterCallback_IsFinished()
    {
        bool IsFinished = CMPluginAPI.CMPluginRegisterUpdateHierarchy(ServerAddr, IntPtr.Zero, GetCurHierCallback);
        return !IsFinished;
    }

    IEnumerator AsyRegisterCallBack_GetVrpnDataOrder()
    {
        yield return new WaitWhile(RegisterCallback_IsFinished);
        IsRegisterCallBack_Finished = true;
        Debug.Log("regesit success");
    }

    [MonoPInvokeCallback(typeof(ChingMU.CMPluginAPI.UpdateHierarchyCallback))]
    public void GetClientThisHumanHierarchy(IntPtr CallBackFun_agrs, CMPluginAPI.VrpnHierarchy CurHierarchy)
    {
        CurCharacterHierResult = CurHierarchy;

        if (serverType == "MCAvatar")
        {
            int startIndex = (ObjectID_InCMTrackSence * 150 + 300);//ObjectID_InCMTrackSence * 150 + 100
            int endIndex = startIndex + 150;//150
            if ((startIndex <= CurHierarchy.sensor) && (CurHierarchy.sensor < endIndex))
            {
                if (UnityCharAllTransNodeAndNameMap.ContainsKey(CurHierarchy.name))
                {
                    int ChingMUClent_boneId = (CurHierarchy.sensor - 300) % 150;//(CurCharacterHierResult.sensor - 100) % 150;
                    if (CharAllTransNode.Count > ChingMUClent_boneId)
                    {
                        CharAllTransNode[ChingMUClent_boneId] = UnityCharAllTransNodeAndNameMap[CurHierarchy.name];
                    }
                }
            }
        }

        if (serverType == "MCServer")
        {
            int startIndex = (ObjectID_InCMTrackSence * 150 + 100);//ObjectID_InCMTrackSence * 150 + 100
            int endIndex = startIndex + 150;//150
            if ((startIndex <= CurHierarchy.sensor) && (CurHierarchy.sensor < endIndex))
            {
                if (UnityCharAllTransNodeAndNameMap.ContainsKey(CurHierarchy.name))
                {
                    int ChingMUClent_boneId = (CurHierarchy.sensor - 100) % 150;//(CurCharacterHierResult.sensor - 100) % 150;
                    if (CharAllTransNode.Count > ChingMUClent_boneId)
                    {
                        CharAllTransNode[ChingMUClent_boneId] = UnityCharAllTransNodeAndNameMap[CurHierarchy.name];
                    }
                }
            }
        }   
    }

    private void OnDestroy()
    {
        Marshal.FreeHGlobal(callback_args);
    }

    void FixedUpdate()
    {
        /*
         当追踪client里的角色列表里的角色层级发生变动，亦或者是管理列表中的对象被删除或者有新对象添加时，或者在第一次连接追踪client时，每在追踪client里面都会触发一个事件，
         这个事件就是，遍历追踪client场景里的所有对象（刚体，与huamn中的每个网格对象，以及骨骼节点对象），当遍历每个对象时就会调用事先注册的回调函数，把当前遍历的节点信息通过回调函数返回
         */
        //用重定向数据驱动
      
        if (IsRegisterCallBack_Finished)
        {
            bool IsTrackedHuman = CMPlugin.GetHumanWithRetargetPose(ObjectID_InCMTrackSence, JointLocalPos, JointLocalRot);
            if (IsTrackedHuman) 
            {
                for (int i = 0; i < CharAllTransNode.Count; i++)
                {
                     if (CharAllTransNode[i] != null)
                    {
                        CharAllTransNode[i].localRotation = JointLocalRot[i];
                        CharAllTransNode[i].localPosition = JointLocalPos[i];
                    }
                }
            }
        }
    }

    void GetRetargetDataMapTransHierarchy(Transform CurBoneJointTrans)//第一帧深度递归获取对应骨骼节点的transform;
    {
        HuamnJointTrans.Add(CurBoneJointTrans);
        for (int i = 0; i < CurBoneJointTrans.childCount; i++)
        {
            GetRetargetDataMapTransHierarchy(CurBoneJointTrans.GetChild(i));
        }
    }

    void GetCMserverType()
    {
        string[] str = ServerAddr.Split('@');
        if (str[0] == "MCAvatar")
        {
            serverType = "MCAvatar";
        }
        else
            serverType = "MCServer";
    }
}
