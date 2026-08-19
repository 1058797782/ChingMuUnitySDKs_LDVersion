using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using ChingMU;
using UnityEngine;

public class SyncHumanForLiveStream : MonoBehaviour
{
    private const int MaximumSegmentCount = 150;

    private readonly Quaternion[] segmentRotations = new Quaternion[MaximumSegmentCount];
    private readonly List<int> humanIds = new List<int>();
    private readonly List<GameObject> humanObjects = new List<GameObject>();
    private readonly List<Dictionary<int, Transform>> segmentTransforms = new List<Dictionary<int, Transform>>();
    private readonly List<int> rootSegmentIds = new List<int>();
    private readonly List<List<MeshRenderer>> humanMarkerRenderers = new List<List<MeshRenderer>>();
    private readonly HashSet<int> reservedHumanIds = new HashSet<int>();
    private readonly object humanSync = new object();
    private readonly ChingMuCallbackQueue callbackQueue = new ChingMuCallbackQueue();

    public bool showHumanMarker;
    public Material MarkColorM;

    private CMPluginCommonInterface plugin;
    private CMPluginAPI.callbackDelegate createHumanCallback;
    private CMPluginAPI.callbackDelegate deleteHumanCallback;
    private IntPtr callbackToken;
    private bool createRegistered;
    private bool deleteRegistered;
    private bool previousMarkerVisibility;
    private MaterialPropertyBlock colorProperties;

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        if (plugin == null || plugin.cMpluginType != CMPluginAPI.CMPluginType.LiveStream)
        {
            return;
        }

        colorProperties = new MaterialPropertyBlock();
        previousMarkerVisibility = !showHumanMarker;
        callbackToken = ChingMuCallbackRegistry.Register(this);
        createHumanCallback = OnCreateHuman;
        deleteHumanCallback = OnDeleteHuman;
        createRegistered = CMPluginAPI.RegisterCallback(
            CMPluginAPI.CallbackType.CREATE_HUMAN,
            createHumanCallback,
            callbackToken);
        deleteRegistered = CMPluginAPI.RegisterCallback(
            CMPluginAPI.CallbackType.DELETE_HUMAN,
            deleteHumanCallback,
            callbackToken);

        if (!createRegistered || !deleteRegistered)
        {
            Debug.LogWarning("One or more ChingMu human callbacks could not be registered.", this);
        }
    }

    [MonoPInvokeCallback(typeof(CMPluginAPI.callbackDelegate))]
    private static void OnCreateHuman(IntPtr userdata, IntPtr info)
    {
        SyncHumanForLiveStream target;
        if (info == IntPtr.Zero || !ChingMuCallbackRegistry.TryGet(userdata, out target))
        {
            return;
        }

        CMPluginAPI.aHumanInfo humanInfo = Marshal.PtrToStructure<CMPluginAPI.aHumanInfo>(info);
        if (target.TryReserveHuman(humanInfo.humanID))
        {
            target.callbackQueue.Enqueue(() => target.CreateHuman(humanInfo));
        }
    }

    [MonoPInvokeCallback(typeof(CMPluginAPI.callbackDelegate))]
    private static void OnDeleteHuman(IntPtr userdata, IntPtr info)
    {
        SyncHumanForLiveStream target;
        if (info == IntPtr.Zero || !ChingMuCallbackRegistry.TryGet(userdata, out target))
        {
            return;
        }

        int humanId = Marshal.ReadInt32(info);
        target.callbackQueue.Enqueue(() => target.DeleteHuman(humanId));
    }

    private void FixedUpdate()
    {
        callbackQueue.Drain();
        UpdateMarkerVisibility();
        if (plugin == null)
        {
            return;
        }

        for (int humanIndex = 0; humanIndex < humanIds.Count; humanIndex++)
        {
            Vector3 humanPosition;
            if (!plugin.GetHumanWithoutRetargetPose(humanIds[humanIndex], out humanPosition, segmentRotations))
            {
                continue;
            }

            Dictionary<int, Transform> currentSegments = segmentTransforms[humanIndex];
            Transform root;
            if (currentSegments.TryGetValue(rootSegmentIds[humanIndex], out root))
            {
                root.position = humanPosition;
            }

            foreach (KeyValuePair<int, Transform> pair in currentSegments)
            {
                if (pair.Key >= 0 && pair.Key < segmentRotations.Length && pair.Value != null)
                {
                    pair.Value.localRotation = segmentRotations[pair.Key];
                }
            }
        }
    }

    private bool TryReserveHuman(int humanId)
    {
        lock (humanSync)
        {
            return reservedHumanIds.Add(humanId);
        }
    }

    private void CreateHuman(CMPluginAPI.aHumanInfo humanInfo)
    {
        if (humanInfo.segmentInfo == null)
        {
            ReleaseHumanReservation(humanInfo.humanID);
            return;
        }

        string humanName = string.IsNullOrEmpty(humanInfo.humanName)
            ? "Human " + humanInfo.humanID
            : humanInfo.humanName;
        GameObject human = new GameObject(humanName);
        human.transform.SetParent(transform, false);

        Dictionary<int, Transform> transforms = new Dictionary<int, Transform>();
        Dictionary<int, CMPluginAPI.aSegmentInfo> segments = new Dictionary<int, CMPluginAPI.aSegmentInfo>();
        List<MeshRenderer> markerRenderers = new List<MeshRenderer>();
        int segmentCount = Math.Min(MaximumSegmentCount, Math.Min(Math.Max(humanInfo.segmentNum, 0), humanInfo.segmentInfo.Length));
        int rootId = 0;
        Color color = HumanColor(humanInfo.rgb);

        for (int index = 0; index < segmentCount; index++)
        {
            CMPluginAPI.aSegmentInfo segment = humanInfo.segmentInfo[index];
            int segmentId = segment.index >= 0 && segment.index < MaximumSegmentCount ? segment.index : index;
            if (transforms.ContainsKey(segmentId))
            {
                continue;
            }

            GameObject joint = CreateJoint(segmentId < 23, segment.name, color);
            joint.transform.SetParent(human.transform, false);
            transforms.Add(segmentId, joint.transform);
            segments.Add(segmentId, segment);
            if (segment.parentId == -1)
            {
                rootId = segmentId;
            }
        }

        foreach (KeyValuePair<int, CMPluginAPI.aSegmentInfo> pair in segments)
        {
            Transform current = transforms[pair.Key];
            CMPluginAPI.aSegmentInfo segment = pair.Value;
            Transform parent;
            if (segment.parentId >= 0 && transforms.TryGetValue(segment.parentId, out parent))
            {
                current.SetParent(parent, false);
                Vector3 nativePosition = segment.posInParent;
                current.localPosition = new Vector3(nativePosition.x, nativePosition.z, nativePosition.y) / 1000f;
                DrawSegment(parent, current, color);
            }
            else
            {
                current.SetParent(human.transform, false);
            }

            CreateMarkers(current, segment, markerRenderers);
        }

        humanIds.Add(humanInfo.humanID);
        humanObjects.Add(human);
        segmentTransforms.Add(transforms);
        rootSegmentIds.Add(rootId);
        humanMarkerRenderers.Add(markerRenderers);
        previousMarkerVisibility = !showHumanMarker;
    }

    private GameObject CreateJoint(bool bodyJoint, string jointName, Color color)
    {
        string resourceName = bodyJoint ? "Point" : "FingerPoint";
        GameObject prefab = Resources.Load<GameObject>(resourceName);
        GameObject joint = prefab != null
            ? Instantiate(prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        joint.name = string.IsNullOrEmpty(jointName) ? "Segment" : jointName;
        if (prefab == null)
        {
            joint.transform.localScale = Vector3.one * (bodyJoint ? 0.025f : 0.0125f);
            RemoveCollider(joint);
        }

        Renderer renderer = joint.GetComponentInChildren<Renderer>();
        ApplyColor(renderer, color);
        return joint;
    }

    private void CreateMarkers(
        Transform parent,
        CMPluginAPI.aSegmentInfo segment,
        List<MeshRenderer> renderers)
    {
        if (segment.markerPos == null)
        {
            return;
        }

        int markerCount = Math.Min(Math.Max(segment.markerNum, 0), segment.markerPos.Length);
        for (int index = 0; index < markerCount; index++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = parent.name + " Marker " + MarkerName(segment.markerNames, index);
            marker.transform.SetParent(parent, false);
            Vector3 nativePosition = segment.markerPos[index];
            marker.transform.localPosition = new Vector3(nativePosition.x, nativePosition.z, nativePosition.y) / 1000f;
            marker.transform.localScale = Vector3.one * 0.015f;
            RemoveCollider(marker);

            MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (MarkColorM != null)
                {
                    renderer.sharedMaterial = MarkColorM;
                }
                renderers.Add(renderer);
            }
        }
    }

    private void DrawSegment(Transform start, Transform end, Color color)
    {
        Vector3 direction = end.position - start.position;
        float length = direction.magnitude;
        if (length <= Mathf.Epsilon)
        {
            return;
        }

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        line.name = start.name + "-" + end.name + " Segment";
        line.transform.position = (start.position + end.position) * 0.5f;
        line.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        line.transform.localScale = new Vector3(0.005f, length * 0.5f, 0.005f);
        line.transform.SetParent(start, true);
        RemoveCollider(line);
        ApplyColor(line.GetComponent<Renderer>(), color);
    }

    private void DeleteHuman(int humanId)
    {
        for (int index = 0; index < humanIds.Count; index++)
        {
            if (humanIds[index] != humanId)
            {
                continue;
            }

            Destroy(humanObjects[index]);
            humanIds.RemoveAt(index);
            humanObjects.RemoveAt(index);
            segmentTransforms.RemoveAt(index);
            rootSegmentIds.RemoveAt(index);
            humanMarkerRenderers.RemoveAt(index);
            break;
        }

        ReleaseHumanReservation(humanId);
    }

    private void ReleaseHumanReservation(int humanId)
    {
        lock (humanSync)
        {
            reservedHumanIds.Remove(humanId);
        }
    }

    private void UpdateMarkerVisibility()
    {
        if (previousMarkerVisibility == showHumanMarker)
        {
            return;
        }

        for (int humanIndex = 0; humanIndex < humanMarkerRenderers.Count; humanIndex++)
        {
            List<MeshRenderer> renderers = humanMarkerRenderers[humanIndex];
            for (int markerIndex = 0; markerIndex < renderers.Count; markerIndex++)
            {
                if (renderers[markerIndex] != null)
                {
                    renderers[markerIndex].enabled = showHumanMarker;
                }
            }
        }

        previousMarkerVisibility = showHumanMarker;
    }

    private void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        colorProperties.SetColor("_Color", color);
        colorProperties.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(colorProperties);
    }

    private void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static Color HumanColor(int[] rgb)
    {
        return rgb != null && rgb.Length >= 3
            ? new Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f)
            : Color.white;
    }

    private static string MarkerName(byte[] markerNames, int markerIndex)
    {
        if (markerNames == null)
        {
            return markerIndex.ToString();
        }

        const int bytesPerName = 132;
        int offset = markerIndex * bytesPerName;
        if (offset < 0 || offset >= markerNames.Length)
        {
            return markerIndex.ToString();
        }

        int count = Math.Min(bytesPerName, markerNames.Length - offset);
        string value = Encoding.UTF8.GetString(markerNames, offset, count).TrimEnd('\0');
        return string.IsNullOrEmpty(value) ? markerIndex.ToString() : value;
    }

    private void OnDestroy()
    {
        UnregisterCallback(CMPluginAPI.CallbackType.CREATE_HUMAN, createHumanCallback, createRegistered);
        UnregisterCallback(CMPluginAPI.CallbackType.DELETE_HUMAN, deleteHumanCallback, deleteRegistered);
        ChingMuCallbackRegistry.Unregister(callbackToken);
        callbackToken = IntPtr.Zero;
        callbackQueue.Clear();

        for (int index = 0; index < humanObjects.Count; index++)
        {
            if (humanObjects[index] != null)
            {
                Destroy(humanObjects[index]);
            }
        }
    }

    private void UnregisterCallback(
        CMPluginAPI.CallbackType type,
        CMPluginAPI.callbackDelegate callback,
        bool registered)
    {
        if (!registered || callback == null)
        {
            return;
        }

        try
        {
            CMPluginAPI.UnRegisterCallback(type, callback);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("ChingMu human callback could not be unregistered: " + exception.Message, this);
        }
    }
}
