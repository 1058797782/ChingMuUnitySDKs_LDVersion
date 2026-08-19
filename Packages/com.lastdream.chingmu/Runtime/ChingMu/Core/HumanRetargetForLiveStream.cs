using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ChingMU;
using UnityEngine;

public class HumanRetargetForLiveStream : MonoBehaviour
{
    public int humanID;
    List<Transform> HumanJointTrans;
    Dictionary<string, Transform> UnityCharAllTransNodeAndNameMap;
    List<Transform> CharAllTransNode = new List<Transform>();
    List<Vector3> CharAllTransNodeInitLocalPos = new List<Vector3>();
    private GCHandle handle1;

    Quaternion[] rot = new Quaternion[150];
    Vector3[] pos = new Vector3[150];
   
    List<System.Action> actionList = new List<System.Action>();
    bool IsFinishedCallback = false;

    CMPluginCommonInterface CMPlugin;

    CMPluginAPI.callbackDelegate createHumanFunc;

    void Start()
    {
        if (CMPluginThreadManager.CMPlugin == null)
        {
            return;
        }

        handle1 = GCHandle.Alloc(this);
        System.IntPtr userdata = GCHandle.ToIntPtr(handle1);
        CMPlugin = CMPluginThreadManager.CMPlugin;

        createHumanFunc = new CMPluginAPI.callbackDelegate(CreateHumanRetargetCallbackFunc);      

        bool IsRegister = CMPluginAPI.RegisterCallback(CMPluginAPI.CallbackType.CREATE_HUMAN, createHumanFunc, userdata);

        HumanJointTrans = new List<Transform>();
        GetRetargetDataMapTransHierarchy(transform);
        UnityCharAllTransNodeAndNameMap = new Dictionary<string, Transform>();
        foreach (Transform var in HumanJointTrans)
        {
            CharAllTransNodeInitLocalPos.Add(Vector3.zero);
            CharAllTransNode.Add(null);
            UnityCharAllTransNodeAndNameMap.Add(var.gameObject.name, var);
        }
        
    }

    void CreateHumanRetargetCallbackFunc(System.IntPtr userdata, System.IntPtr info)
    {
        GCHandle handle2 = GCHandle.FromIntPtr(userdata);
        HumanRetargetForLiveStream monitor = (HumanRetargetForLiveStream)handle2.Target;

        CMPluginAPI.aHumanInfo humanInfo = (CMPluginAPI.aHumanInfo)Marshal.PtrToStructure(info, typeof(CMPluginAPI.aHumanInfo));
        //Debug.Log("  CreateHumanRetargetCallbackFunc  xxxxxxxxx " + "name: " + humanInfo.humanName + " id: " + humanInfo.humanID + "  ScriptGameName " + monitor.name);
        if (humanInfo.humanID == monitor.humanID)
        {
            monitor.actionList.Add(delegate
            {

                for (int i = 0; i < humanInfo.segmentNum; ++i)
                {
                   // Debug.Log("name:" + humanInfo.segmentInfo[i].name + " id:" + humanInfo.segmentInfo[i].index + " parentId:" + humanInfo.segmentInfo[i].parentId);
                    if (monitor.UnityCharAllTransNodeAndNameMap.ContainsKey(humanInfo.segmentInfo[i].name))
                    {
                        monitor.CharAllTransNode[humanInfo.segmentInfo[i].index] = monitor.UnityCharAllTransNodeAndNameMap[humanInfo.segmentInfo[i].name];
                        //monitor.CharAllTransNode[humanInfo.segmentInfo[i].index].localPosition = new Vector3(humanInfo.segmentInfo[i].posInParent.x,
                        //humanInfo.segmentInfo[i].posInParent.z, humanInfo.segmentInfo[i].posInParent.y) / 1000;
                        pos[humanInfo.segmentInfo[i].index] = new Vector3(humanInfo.segmentInfo[i].posInParent.x,
                        humanInfo.segmentInfo[i].posInParent.z, humanInfo.segmentInfo[i].posInParent.y) / 1000;
                    }
                }
                monitor.IsFinishedCallback = true;
                //Debug.Log("  CreateHumanRetargetCallbackFunc  xxxxxxxxx " + "name: "+ humanInfo.humanName+" id: "+humanInfo.humanID +"  ScriptGameName "+monitor.name);
            });
           
        }

    }    

    void FixedUpdate()
    {
        for (int i = 0; i < actionList.Count; ++i)
        {
            actionList[i]();
        }

        actionList.Clear();

        if (IsFinishedCallback) 
        {

            bool  IsDectected =CMPlugin.GetHumanWithRetargetPose(humanID, pos, rot);

            if(IsDectected)
            {
                for (int i = 1; i < CharAllTransNode.Count; i++)
                {
                    if (CharAllTransNode[i] != null)
                    {
                        CharAllTransNode[i].localRotation = rot[i - 1];
                        CharAllTransNode[i].localPosition = pos[i];
                    }
                }

            }
        }
      

    }

    void GetRetargetDataMapTransHierarchy(Transform CurBoneJointTrans)//第一帧深度递归获取对应骨骼节点的transform;
    {

        HumanJointTrans.Add(CurBoneJointTrans);
        for (int i = 0; i < CurBoneJointTrans.childCount; i++)
        {
            GetRetargetDataMapTransHierarchy(CurBoneJointTrans.GetChild(i));
        }
    }

    private void OnDestroy()
    {
        handle1.Free();
    }
}