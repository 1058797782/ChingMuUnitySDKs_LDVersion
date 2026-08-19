using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using ChingMU;
using UnityEngine;

public class HumanRetargetForLiveStream : MonoBehaviour
{
    private const int MaximumSegmentCount = 150;

    public int humanID;

    private readonly List<Transform> humanJointTransforms = new List<Transform>();
    private readonly Dictionary<string, Transform> transformsByName = new Dictionary<string, Transform>();
    private readonly Transform[] transformsBySegment = new Transform[MaximumSegmentCount];
    private readonly Quaternion[] rotations = new Quaternion[MaximumSegmentCount];
    private readonly Vector3[] positions = new Vector3[MaximumSegmentCount];
    private readonly ChingMuCallbackQueue callbackQueue = new ChingMuCallbackQueue();

    private CMPluginCommonInterface plugin;
    private CMPluginAPI.callbackDelegate createHumanCallback;
    private IntPtr callbackToken;
    private bool callbackRegistered;
    private bool hierarchyReady;

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        if (plugin == null || plugin.cMpluginType != CMPluginAPI.CMPluginType.LiveStream)
        {
            return;
        }

        GetRetargetDataMapTransHierarchy(transform);
        for (int index = 0; index < humanJointTransforms.Count; index++)
        {
            Transform current = humanJointTransforms[index];
            if (!transformsByName.ContainsKey(current.name))
            {
                transformsByName.Add(current.name, current);
            }
        }

        callbackToken = ChingMuCallbackRegistry.Register(this);
        createHumanCallback = OnCreateHuman;
        callbackRegistered = CMPluginAPI.RegisterCallback(
            CMPluginAPI.CallbackType.CREATE_HUMAN,
            createHumanCallback,
            callbackToken);

        if (!callbackRegistered)
        {
            Debug.LogWarning("ChingMu human hierarchy callback could not be registered.", this);
        }
    }

    [MonoPInvokeCallback(typeof(CMPluginAPI.callbackDelegate))]
    private static void OnCreateHuman(IntPtr userdata, IntPtr info)
    {
        HumanRetargetForLiveStream target;
        if (info == IntPtr.Zero || !ChingMuCallbackRegistry.TryGet(userdata, out target))
        {
            return;
        }

        CMPluginAPI.aHumanInfo humanInfo = Marshal.PtrToStructure<CMPluginAPI.aHumanInfo>(info);
        if (humanInfo.humanID == target.humanID)
        {
            target.callbackQueue.Enqueue(() => target.ApplyHierarchy(humanInfo));
        }
    }

    private void FixedUpdate()
    {
        callbackQueue.Drain();
        if (!hierarchyReady || plugin == null)
        {
            return;
        }

        if (!plugin.GetHumanWithRetargetPose(humanID, positions, rotations))
        {
            return;
        }

        for (int index = 1; index < transformsBySegment.Length; index++)
        {
            Transform current = transformsBySegment[index];
            if (current == null)
            {
                continue;
            }

            current.localRotation = rotations[index - 1];
            current.localPosition = positions[index];
        }
    }

    private void ApplyHierarchy(CMPluginAPI.aHumanInfo humanInfo)
    {
        if (humanInfo.segmentInfo == null)
        {
            return;
        }

        int count = Math.Min(MaximumSegmentCount, Math.Min(humanInfo.segmentNum, humanInfo.segmentInfo.Length));
        for (int index = 0; index < count; index++)
        {
            CMPluginAPI.aSegmentInfo segment = humanInfo.segmentInfo[index];
            if (segment.index < 0 || segment.index >= MaximumSegmentCount || string.IsNullOrEmpty(segment.name))
            {
                continue;
            }

            Transform current;
            if (!transformsByName.TryGetValue(segment.name, out current))
            {
                continue;
            }

            transformsBySegment[segment.index] = current;
            Vector3 nativePosition = segment.posInParent;
            positions[segment.index] = new Vector3(nativePosition.x, nativePosition.z, nativePosition.y) / 1000f;
        }

        hierarchyReady = true;
    }

    private void GetRetargetDataMapTransHierarchy(Transform current)
    {
        humanJointTransforms.Add(current);
        for (int index = 0; index < current.childCount; index++)
        {
            GetRetargetDataMapTransHierarchy(current.GetChild(index));
        }
    }

    private void OnDestroy()
    {
        if (callbackRegistered && createHumanCallback != null)
        {
            try
            {
                CMPluginAPI.UnRegisterCallback(CMPluginAPI.CallbackType.CREATE_HUMAN, createHumanCallback);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ChingMu human callback could not be unregistered: " + exception.Message, this);
            }
        }

        callbackRegistered = false;
        ChingMuCallbackRegistry.Unregister(callbackToken);
        callbackToken = IntPtr.Zero;
        createHumanCallback = null;
        callbackQueue.Clear();
    }
}
