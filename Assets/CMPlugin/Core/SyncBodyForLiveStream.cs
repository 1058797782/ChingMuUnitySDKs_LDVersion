using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ChingMU;
using UnityEngine;

public class SyncBodyForLiveStream : MonoBehaviour
{
    // Start is called before the first frame update

    private List<int> bodyIdList = new List<int>();
    private List<GameObject> bodyList = new List<GameObject>();
    private List<Transform> bodyTransformList = new List<Transform>();
    private List<System.Action> actionList = new List<System.Action>();
    private GCHandle handle1;

    Vector3 BodyWpos;
    Quaternion BodyWrot;
    //bool IsFinishedCallback = false;
    void CreateBodyCallbackFunc(System.IntPtr userdata, System.IntPtr info) //每次创建Body的时候，会回调一次这个函数，info是关于创建这个Body的关键信息，类型：aBodyInfo
    {
        GCHandle handle2 = GCHandle.FromIntPtr(userdata);
        SyncBodyForLiveStream monitor = (SyncBodyForLiveStream)handle2.Target;
        CMPluginAPI.aBodyInfo bodyInfo = (CMPluginAPI.aBodyInfo)Marshal.PtrToStructure(info, typeof(CMPluginAPI.aBodyInfo));
        if (!monitor.bodyIdList.Contains(bodyInfo.id))
        {
            monitor.actionList.Add(delegate
            {
                GameObject game = new GameObject(bodyInfo.name);
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(0, 0, 0);
                cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                cube.name = bodyInfo.name + "_soildRender";
                cube.GetComponent<Renderer>().material.color = new Color((float)bodyInfo.rgb[0] / 255, (float)bodyInfo.rgb[1] / 255, (float)bodyInfo.rgb[2] / 255);
                cube.transform.parent = game.transform;

                //GameObject[] bodyMarkers = new GameObject[bodyInfo.markerNum];
                for (int i = 0; i < bodyInfo.markerNum; ++i)
                {
                    GameObject bodyMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bodyMarker.transform.position = new Vector3(bodyInfo.markerPos[i].x, bodyInfo.markerPos[i].z, bodyInfo.markerPos[i].y) / 1000f;
                    bodyMarker.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    bodyMarker.transform.parent = game.transform;
                    bodyMarker.name = bodyInfo.name + "Marker" + i;
                    //string markerName = System.Text.Encoding.Default.GetString(bodyInfo.markerNames, i * 132, 132);
                    //print(markerName);
                }

                LineRenderer lineRenderer = game.AddComponent<LineRenderer>();
                List<Vector3> markerPoints = new List<Vector3>();
                int cnt = 0;
                for (int i = 0; i < bodyInfo.markerNum; ++i)
                {
                    for (int j = i + 1; j < bodyInfo.markerNum; ++j)
                    {
                        markerPoints.Add(new Vector3(bodyInfo.markerPos[i].x, bodyInfo.markerPos[i].z, bodyInfo.markerPos[i].y) / 1000f);
                        markerPoints.Add(new Vector3(bodyInfo.markerPos[j].x, bodyInfo.markerPos[j].z, bodyInfo.markerPos[j].y) / 1000f);
                        cnt += 2;
                    }
                }

                lineRenderer.positionCount = cnt;
                lineRenderer.SetPositions(markerPoints.ToArray());
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = new Color((float)bodyInfo.rgb[0] / 255, (float)bodyInfo.rgb[1] / 255, (float)bodyInfo.rgb[2] / 255);
                lineRenderer.endColor = new Color((float)bodyInfo.rgb[0] / 255, (float)bodyInfo.rgb[1] / 255, (float)bodyInfo.rgb[2] / 255);
                lineRenderer.useWorldSpace = false;

                lineRenderer.startWidth = 0.005f;
                lineRenderer.endWidth = 0.005f;

                monitor.bodyList.Add(game);
                monitor.bodyTransformList.Add(game.transform);
                monitor.bodyIdList.Add(bodyInfo.id);
                print("create body " + bodyInfo.name);
            });
        }
    }

    public void DeleteBodyCallbackFunc(System.IntPtr userdata, System.IntPtr info) //删除刚体的时候，会回调一次这个函数，info，被删除刚体的id;
    {
        GCHandle handle2 = GCHandle.FromIntPtr(userdata);
        SyncBodyForLiveStream monitor = (SyncBodyForLiveStream)handle2.Target;

        int deletedBodyID = (int)Marshal.PtrToStructure(info, typeof(int));

        monitor.actionList.Add(delegate
        {
            bool ret = false;
            for (int i = 0; i < monitor.bodyList.Count; ++i)
            {
                if (deletedBodyID == monitor.bodyIdList[i])
                {
                    Destroy(monitor.bodyList[i]);
                    monitor.bodyList.RemoveAt(i);
                    monitor.bodyIdList.RemoveAt(i);
                    monitor.bodyTransformList.RemoveAt(i);
                    ret = true;
                    print("delete body id : " + deletedBodyID);
                    break;
                }
            }
            if (!ret)
            {
                print("delete body id not found: " + deletedBodyID);
            }
        });
    }

    void Start()
    {
                                                 
        if (CMPluginThreadManager.CMPlugin == null)
        {
            return;
        }
        handle1 = GCHandle.Alloc(this);
        System.IntPtr userdata = GCHandle.ToIntPtr(handle1);
        // create body register
        CMPluginAPI.callbackDelegate createBodyFunc = new CMPluginAPI.callbackDelegate(CreateBodyCallbackFunc);
        bool IsRegister = CMPluginAPI.RegisterCallback(CMPluginAPI.CallbackType.CREATE_BODY, createBodyFunc, userdata);

        // delete body register
        CMPluginAPI.callbackDelegate deleteBodyFunc = new CMPluginAPI.callbackDelegate(DeleteBodyCallbackFunc);
        IsRegister = CMPluginAPI.RegisterCallback(CMPluginAPI.CallbackType.DELETE_BODY, deleteBodyFunc, userdata);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < actionList.Count; ++i)
        {
            actionList[i]();
        }
        actionList.Clear();

        if (CMPluginThreadManager.CMPlugin == null)
        {
            return;
        }
        else 
        {
            if (CMPluginThreadManager.CMPlugin.cMpluginType !=CMPluginAPI.CMPluginType.LiveStream) 
            {
                return;
            }
        }
        CMPluginAPI.tFrame frameData = (CMPluginAPI.tFrame)Marshal.PtrToStructure(CMPluginAPI.GetFrameData(), typeof(CMPluginAPI.tFrame));

        for (int k = 0; k < bodyIdList.Count; ++k)
        {
            CMPluginThreadManager.CMPlugin.GetTrackerPose(bodyIdList[k], out BodyWpos, out BodyWrot);
            bodyTransformList[k].position = BodyWpos;
            bodyTransformList[k].rotation = BodyWrot;

        }
    }
    private void OnDestroy()
    {
        //CMPluginAPI.UnRegisterCallback(CMPluginAPI.CallbackType.CREATE_HUMAN, createHumanFunc);
        handle1.Free();
    }
}
