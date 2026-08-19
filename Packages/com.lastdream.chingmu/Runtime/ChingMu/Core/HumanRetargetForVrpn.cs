using System;
using System.Collections;
using System.Collections.Generic;
using AOT;
using ChingMU;
using UnityEngine;

public class HumanRetargetForVrpn : MonoBehaviour
{
    private const int MaximumSegmentCount = 150;

    private readonly Vector3[] jointLocalPositions = new Vector3[MaximumSegmentCount];
    private readonly Quaternion[] jointLocalRotations = new Quaternion[MaximumSegmentCount];
    private readonly Transform[] transformsBySegment = new Transform[MaximumSegmentCount];
    private readonly Dictionary<string, Transform> transformsByName = new Dictionary<string, Transform>();
    private readonly ChingMuCallbackQueue callbackQueue = new ChingMuCallbackQueue();
    private readonly WaitForSeconds callbackRetryDelay = new WaitForSeconds(0.25f);

    private CMPluginCommonInterface plugin;
    private CMPluginAPI.UpdateHierarchyCallback hierarchyCallback;
    private IntPtr callbackToken;
    private string serverAddress = string.Empty;
    private CMPluginAPI.CMServerType serverType;
    private bool callbackRegistered;

    [Header("ChingMUTrackerSeting")]
    [Tooltip("ID is Tracker Client manger list Order index")]
    public int ObjectID_InCMTrackSence;

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        if (plugin == null || plugin.cMpluginType != CMPluginAPI.CMPluginType.Vrpn)
        {
            return;
        }

        serverAddress = ChingMuAddress.Build(plugin.ServerIp, plugin.Port);
        serverType = ChingMuAddress.ServerType(plugin.ServerIp);
        CollectTransforms(transform);

        callbackToken = ChingMuCallbackRegistry.Register(this);
        hierarchyCallback = OnHierarchy;
        StartCoroutine(RegisterHierarchyCallback());
    }

    private IEnumerator RegisterHierarchyCallback()
    {
        while (isActiveAndEnabled && !callbackRegistered)
        {
            callbackRegistered = CMPluginAPI.CMPluginRegisterUpdateHierarchy(
                serverAddress,
                callbackToken,
                hierarchyCallback);
            if (!callbackRegistered)
            {
                yield return callbackRetryDelay;
            }
        }
    }

    [MonoPInvokeCallback(typeof(CMPluginAPI.UpdateHierarchyCallback))]
    private static void OnHierarchy(IntPtr callbackArgs, CMPluginAPI.VrpnHierarchy hierarchy)
    {
        HumanRetargetForVrpn target;
        if (ChingMuCallbackRegistry.TryGet(callbackArgs, out target))
        {
            target.callbackQueue.Enqueue(() => target.ApplyHierarchy(hierarchy));
        }
    }

    private void FixedUpdate()
    {
        callbackQueue.Drain();
        if (!callbackRegistered || plugin == null)
        {
            return;
        }

        if (!plugin.GetHumanWithRetargetPose(
                ObjectID_InCMTrackSence,
                jointLocalPositions,
                jointLocalRotations))
        {
            return;
        }

        for (int index = 0; index < transformsBySegment.Length; index++)
        {
            Transform current = transformsBySegment[index];
            if (current == null)
            {
                continue;
            }

            current.localRotation = jointLocalRotations[index];
            current.localPosition = jointLocalPositions[index];
        }
    }

    private void ApplyHierarchy(CMPluginAPI.VrpnHierarchy hierarchy)
    {
        int baseIndex = serverType == CMPluginAPI.CMServerType.MCAvatar ? 300 : 100;
        int startIndex = ObjectID_InCMTrackSence * MaximumSegmentCount + baseIndex;
        int endIndex = startIndex + MaximumSegmentCount;
        if (hierarchy.sensor < startIndex || hierarchy.sensor >= endIndex || string.IsNullOrEmpty(hierarchy.name))
        {
            return;
        }

        Transform current;
        if (!transformsByName.TryGetValue(hierarchy.name, out current))
        {
            return;
        }

        int segmentIndex = (hierarchy.sensor - baseIndex) % MaximumSegmentCount;
        if (segmentIndex >= 0 && segmentIndex < transformsBySegment.Length)
        {
            transformsBySegment[segmentIndex] = current;
        }
    }

    public void GetClientThisHumanHierarchy(
        IntPtr CallBackFun_agrs,
        CMPluginAPI.VrpnHierarchy CurHierarchy)
    {
        callbackQueue.Enqueue(() => ApplyHierarchy(CurHierarchy));
    }

    private void CollectTransforms(Transform current)
    {
        if (!transformsByName.ContainsKey(current.name))
        {
            transformsByName.Add(current.name, current);
        }

        for (int index = 0; index < current.childCount; index++)
        {
            CollectTransforms(current.GetChild(index));
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        ChingMuCallbackRegistry.Unregister(callbackToken);
        callbackToken = IntPtr.Zero;
        hierarchyCallback = null;
        callbackQueue.Clear();
    }
}
