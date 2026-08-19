using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ChingMU;
using UnityEngine;

public class SyncHumanForLiveStream : MonoBehaviour
{
    // Start is called before the first frame update
    // human
    Vector3 humanPos = new Vector3();
    Quaternion[] segmentRot = new Quaternion[150];

    private List<int> humanID = new List<int>();
    private List<GameObject> humanList = new List<GameObject>();
    private List<Dictionary<int, Transform>> segmentTransformList = new List<Dictionary<int, Transform>>();
    private List<System.Action> actionList = new List<System.Action>();
    List<MeshRenderer> HumanMarks = new List<MeshRenderer>();
    private GCHandle handle1;
    public bool showHumanMarker;
    public Material MarkColorM;
    //public Material JointColorM;


    void CreateHumanCallbackFunc(System.IntPtr userdata, System.IntPtr info) //每次创建Human的时候，会回调一次这个函数，info是关于创建这个人的关键信息，类型：aHumanInfo
    {
        GCHandle handle2 = GCHandle.FromIntPtr(userdata);
        SyncHumanForLiveStream monitor = (SyncHumanForLiveStream)handle2.Target;
        CMPluginAPI.aHumanInfo humanInfo = (CMPluginAPI.aHumanInfo)Marshal.PtrToStructure(info, typeof(CMPluginAPI.aHumanInfo));

        if (!monitor.humanID.Contains(humanInfo.humanID))
        {
            monitor.actionList.Add(delegate
            {
                Object prefabObj = Resources.Load("Point");
                Object fingerPrefabObj = Resources.Load("FingerPoint");
                if (prefabObj == null || fingerPrefabObj == null)
                {
                    print("null prefabObj");
                }

                Queue<int> segmentQueue = new Queue<int>();
                Queue<GameObject> sphereQueue = new Queue<GameObject>();
                GameObject human = new GameObject(humanInfo.humanName);

                Dictionary<int, Transform> tmp = new Dictionary<int, Transform>();
                monitor.segmentTransformList.Add(tmp);

                for (int i = 0; i < humanInfo.segmentNum; ++i)
                {
                    if (humanInfo.segmentInfo[i].parentId == -1)
                    {
                        segmentQueue.Enqueue(i);
                        GameObject parentSphere = (GameObject)GameObject.Instantiate(prefabObj, new Vector3(0, 0, 0), Quaternion.identity);
                        parentSphere.name = humanInfo.segmentInfo[i].name;
                        parentSphere.transform.position = new Vector3(humanInfo.rootPos.x, humanInfo.rootPos.z, humanInfo.rootPos.y) / 1000f;
                        parentSphere.transform.parent = human.transform;
                        parentSphere.transform.GetChild(0).name = parentSphere.name + "_soildRender";
                        //parentSphere.GetComponent<Renderer>().material.color = new Color((float)humanInfo.rgb[0] / 255, (float)humanInfo.rgb[1] / 255, (float)humanInfo.rgb[2] / 255);

                        //if (monitor.showHumanMarker)
                        //{
                        GameObject humanMarkers = new GameObject(humanInfo.segmentInfo[i].name + "Marker");
                        humanMarkers.transform.parent = parentSphere.transform;
                        humanMarkers.transform.localPosition = Vector3.zero;
                        humanMarkers.name = parentSphere.name + " marks parent";
                        for (int j = 0; j < humanInfo.segmentInfo[i].markerNum; ++j)
                        {
                            GameObject humanMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            string markerName = System.Text.Encoding.Default.GetString(humanInfo.segmentInfo[i].markerNames, 64 * j, 64);
                            humanMarker.name = parentSphere.name + "_mark" + "_" + markerName;
                            humanMarker.transform.parent = humanMarkers.transform;
                            //humanMarker.transform.parent = parentSphere.transform;
                            humanMarker.transform.localPosition = new Vector3(humanInfo.segmentInfo[i].markerPos[j].x, humanInfo.segmentInfo[i].markerPos[j].z, humanInfo.segmentInfo[i].markerPos[j].y) / 1000f;
                            humanMarker.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
                            MeshRenderer markRender = humanMarker.GetComponent<MeshRenderer>();
                            markRender.material = MarkColorM;
                            HumanMarks.Add(markRender);
                        }
                        //}
                        sphereQueue.Enqueue(parentSphere);
                        monitor.segmentTransformList[monitor.segmentTransformList.Count - 1].Add(0, parentSphere.transform);

                        break;
                    }
                }

                while (segmentQueue.Count != 0)
                {
                    int segmentId = segmentQueue.Dequeue();
                    GameObject parentSphere = sphereQueue.Dequeue();
                    for (int i = 0; i < humanInfo.segmentNum; ++i)
                    {
                        if (humanInfo.segmentInfo[i].parentId == segmentId)
                        {
                            segmentQueue.Enqueue(i);
                            GameObject childSphere;
                            if (i < 23)
                                childSphere = (GameObject)GameObject.Instantiate(prefabObj, new Vector3(0, 0, 0), Quaternion.identity);
                            else
                                childSphere = (GameObject)GameObject.Instantiate(fingerPrefabObj, new Vector3(0, 0, 0), Quaternion.identity);

                            childSphere.name = humanInfo.segmentInfo[i].name;
                            childSphere.transform.GetChild(0).name = childSphere.name + "_soildRender";
                            //childSphere.GetComponent<Renderer>().material.color = new Color((float)humanInfo.rgb[0] / 255, (float)humanInfo.rgb[1] / 255, (float)humanInfo.rgb[2] / 255);

                            childSphere.transform.parent = parentSphere.transform;
                            childSphere.transform.localPosition = new Vector3(humanInfo.segmentInfo[i].posInParent.x, humanInfo.segmentInfo[i].posInParent.z, humanInfo.segmentInfo[i].posInParent.y) / 1000f;
                            DrawLS(parentSphere, childSphere, new Color((float)humanInfo.rgb[0] / 255, (float)humanInfo.rgb[1] / 255, (float)humanInfo.rgb[2] / 255));

                            //if (monitor.showHumanMarker)
                            //{
                            GameObject humanMarkers = new GameObject(humanInfo.segmentInfo[i].name + "Marker");
                            humanMarkers.transform.parent = childSphere.transform;
                            humanMarkers.transform.localPosition = Vector3.zero;
                            humanMarkers.name = childSphere.name + " marks parent";
                            for (int j = 0; j < humanInfo.segmentInfo[i].markerNum; ++j)
                            {
                                GameObject humanMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                string markerName = System.Text.Encoding.Default.GetString(humanInfo.segmentInfo[i].markerNames, 64 * j, 64);
                                humanMarker.name = childSphere.name + "_mark" + "_" + markerName;
                                humanMarker.transform.parent = humanMarkers.transform;
                                humanMarker.transform.localPosition = new Vector3(humanInfo.segmentInfo[i].markerPos[j].x, humanInfo.segmentInfo[i].markerPos[j].z, humanInfo.segmentInfo[i].markerPos[j].y) / 1000f;
                                //humanMarker.transform.parent = humanMarkers.transform;
                                humanMarker.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
                                MeshRenderer markRender = humanMarker.GetComponent<MeshRenderer>();
                                markRender.material = MarkColorM;
                                HumanMarks.Add(markRender);
                            }
                            //}
                            sphereQueue.Enqueue(childSphere);
                            monitor.segmentTransformList[monitor.segmentTransformList.Count - 1].Add(i, childSphere.transform);
                        }
                    }
                }
                //human.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                monitor.humanList.Add(human);
                monitor.humanID.Add(humanInfo.humanID);
                print("crate human " + humanInfo.humanID);
                string humanMarkerName = System.Text.Encoding.Default.GetString(humanInfo.segmentInfo[0].markerNames, 0, 132);
                print(humanMarkerName);
            });
        }


    }
    void DrawLS(GameObject startP, GameObject finalP, Color color)
    {
        Vector3 rightPosition = (startP.transform.position + finalP.transform.position) / 2;
        Vector3 rightRotation = finalP.transform.position - startP.transform.position;
        float HalfLength = Vector3.Distance(startP.transform.position, finalP.transform.position) / 2;

        GameObject MyLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        MyLine.name = startP.name + "-" + finalP.name + "_segment";
        MyLine.gameObject.transform.parent = startP.transform;
        MyLine.transform.position = rightPosition;
        MyLine.transform.rotation = Quaternion.FromToRotation(Vector3.up, rightRotation);
        MyLine.transform.localScale = new Vector3(0.005f, HalfLength, 0.005f);
        MyLine.GetComponent<Renderer>().material.color = color;
    }

    void DeleteHumanCallbackFunc(System.IntPtr userdata, System.IntPtr info)//删除huamn的时候，会回调一次这个函数，info，被删除人的id;
    {
        GCHandle handle2 = GCHandle.FromIntPtr(userdata);
        SyncHumanForLiveStream monitor = (SyncHumanForLiveStream)handle2.Target;
        int deletedHumanId = (int)Marshal.PtrToStructure(info, typeof(int));

        monitor.actionList.Add(delegate
        {
            bool ret = false;

            for (int i = 0; i < monitor.humanList.Count; ++i)
            {
                if (deletedHumanId == monitor.humanID[i])
                {
                    Destroy(monitor.humanList[i]);
                    monitor.humanList.RemoveAt(i);
                    monitor.humanID.RemoveAt(i);
                    monitor.segmentTransformList.RemoveAt(i);
                    ret = true;
                    print("delete human id: " + deletedHumanId);
                    break;
                }
            }
            if (!ret)
            {
                print("delete human id not found: " + deletedHumanId);
            }
        }
        );
    }

    void Start()
    {
        handle1 = GCHandle.Alloc(this);
        System.IntPtr userdata = GCHandle.ToIntPtr(handle1);
 
        if (CMPluginThreadManager.CMPlugin != null)
        {
            // create human register
            CMPluginAPI.callbackDelegate createHumanFunc = new CMPluginAPI.callbackDelegate(CreateHumanCallbackFunc);
            bool IsRegister = CMPluginAPI.RegisterCallback(CMPluginAPI.CallbackType.CREATE_HUMAN, createHumanFunc, userdata);

            // delete human register
            CMPluginAPI.callbackDelegate deleteHumanFunc = new CMPluginAPI.callbackDelegate(DeleteHumanCallbackFunc);
            CMPluginAPI.RegisterCallback(CMPluginAPI.CallbackType.DELETE_HUMAN, deleteHumanFunc, userdata);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < actionList.Count; ++i)
        {
            actionList[i]();
        }
        actionList.Clear();
        foreach (MeshRenderer  v in HumanMarks) 
        {
            v.enabled = showHumanMarker;
        }
        //CMPluginAPI.tFrame frameData = (CMPluginAPI.tFrame)Marshal.PtrToStructure(CMPluginAPI.GetFrameData(), typeof(CMPluginAPI.tFrame));

        for (int k = 0; k < humanID.Count; ++k)
        {

             CMPluginThreadManager.CMPlugin.GetHumanWithoutRetargetPose(humanID[k],out humanPos,segmentRot);

            segmentTransformList[k][0].position = humanPos;
            humanPos = Vector3.zero;
            for (int i = 0; i < segmentTransformList[k].Count; ++i)
            {
                segmentTransformList[k][i].localRotation = segmentRot[i];
                segmentRot[i] = Quaternion.identity;
            }
    
        }
        /*
        for (int k = 0; k < humanList.Count; ++k)
        {
            bool flag = false;
            if (frameData.humanData[k].isDetect == 1)
            {
                for (int i = 0; i < frameData.humanNum; ++i)
                {
                    if (frameData.humanData[i].id == humanID[k])
                    {
                        CMPluginAPI.HumanAttitudeLiveStream(frameData, i, out humanPos, segmentRot);
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    print("invalid human id " + humanID[k]);
                }
                else
                {
                    segmentTransformList[k][0].position = humanPos;

                    for (int i = 0; i < frameData.humanData[k].segementNum; ++i)
                    {
                        segmentTransformList[k][i].localRotation = segmentRot[i];
                    }
                }
            }
        }
        */

    }


    private void OnDestroy()
    {
        handle1.Free();
    }
}
