using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using ChingMU;
using UnityEngine;

public class SyncBodyForLiveStream : MonoBehaviour
{
    private readonly List<int> bodyIds = new List<int>();
    private readonly List<GameObject> bodyObjects = new List<GameObject>();
    private readonly List<Transform> bodyTransforms = new List<Transform>();
    private readonly HashSet<int> reservedBodyIds = new HashSet<int>();
    private readonly object bodySync = new object();
    private readonly ChingMuCallbackQueue callbackQueue = new ChingMuCallbackQueue();

    private CMPluginCommonInterface plugin;
    private CMPluginAPI.callbackDelegate createBodyCallback;
    private CMPluginAPI.callbackDelegate deleteBodyCallback;
    private IntPtr callbackToken;
    private bool createRegistered;
    private bool deleteRegistered;
    private Material lineMaterial;
    private MaterialPropertyBlock colorProperties;

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        if (plugin == null || plugin.cMpluginType != CMPluginAPI.CMPluginType.LiveStream)
        {
            return;
        }

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader != null)
        {
            lineMaterial = new Material(lineShader) { name = "ChingMu Body Lines" };
        }
        colorProperties = new MaterialPropertyBlock();

        callbackToken = ChingMuCallbackRegistry.Register(this);
        createBodyCallback = OnCreateBody;
        deleteBodyCallback = OnDeleteBody;
        createRegistered = CMPluginAPI.RegisterCallback(
            CMPluginAPI.CallbackType.CREATE_BODY,
            createBodyCallback,
            callbackToken);
        deleteRegistered = CMPluginAPI.RegisterCallback(
            CMPluginAPI.CallbackType.DELETE_BODY,
            deleteBodyCallback,
            callbackToken);

        if (!createRegistered || !deleteRegistered)
        {
            Debug.LogWarning("One or more ChingMu body callbacks could not be registered.", this);
        }
    }

    [MonoPInvokeCallback(typeof(CMPluginAPI.callbackDelegate))]
    private static void OnCreateBody(IntPtr userdata, IntPtr info)
    {
        SyncBodyForLiveStream target;
        if (info == IntPtr.Zero || !ChingMuCallbackRegistry.TryGet(userdata, out target))
        {
            return;
        }

        CMPluginAPI.aBodyInfo bodyInfo = Marshal.PtrToStructure<CMPluginAPI.aBodyInfo>(info);
        if (target.TryReserveBody(bodyInfo.id))
        {
            target.callbackQueue.Enqueue(() => target.CreateBody(bodyInfo));
        }
    }

    [MonoPInvokeCallback(typeof(CMPluginAPI.callbackDelegate))]
    private static void OnDeleteBody(IntPtr userdata, IntPtr info)
    {
        SyncBodyForLiveStream target;
        if (info == IntPtr.Zero || !ChingMuCallbackRegistry.TryGet(userdata, out target))
        {
            return;
        }

        int bodyId = Marshal.ReadInt32(info);
        target.callbackQueue.Enqueue(() => target.DeleteBody(bodyId));
    }

    public void DeleteBodyCallbackFunc(IntPtr userdata, IntPtr info)
    {
        if (info != IntPtr.Zero)
        {
            int bodyId = Marshal.ReadInt32(info);
            callbackQueue.Enqueue(() => DeleteBody(bodyId));
        }
    }

    private void FixedUpdate()
    {
        callbackQueue.Drain();
        if (plugin == null)
        {
            return;
        }

        for (int index = 0; index < bodyIds.Count; index++)
        {
            Vector3 position;
            Quaternion rotation;
            plugin.GetTrackerPose(bodyIds[index], out position, out rotation);
            bodyTransforms[index].SetPositionAndRotation(position, rotation);
        }
    }

    private bool TryReserveBody(int bodyId)
    {
        lock (bodySync)
        {
            return reservedBodyIds.Add(bodyId);
        }
    }

    private void CreateBody(CMPluginAPI.aBodyInfo bodyInfo)
    {
        string bodyName = string.IsNullOrEmpty(bodyInfo.name) ? "Body " + bodyInfo.id : bodyInfo.name;
        GameObject root = new GameObject(bodyName);
        root.transform.SetParent(transform, false);

        Color color = BodyColor(bodyInfo.rgb);
        CreateMarker(PrimitiveType.Cube, bodyName + "_solid", Vector3.zero, root.transform, color);

        int markerCount = bodyInfo.markerPos == null
            ? 0
            : Math.Min(Math.Max(bodyInfo.markerNum, 0), bodyInfo.markerPos.Length);
        for (int index = 0; index < markerCount; index++)
        {
            Vector3 nativePosition = bodyInfo.markerPos[index];
            Vector3 position = new Vector3(nativePosition.x, nativePosition.z, nativePosition.y) / 1000f;
            CreateMarker(PrimitiveType.Sphere, bodyName + " Marker " + index, position, root.transform, color);
        }

        if (markerCount > 1)
        {
            LineRenderer lines = root.AddComponent<LineRenderer>();
            int pointCount = markerCount * (markerCount - 1);
            Vector3[] points = new Vector3[pointCount];
            int pointIndex = 0;
            for (int first = 0; first < markerCount; first++)
            {
                for (int second = first + 1; second < markerCount; second++)
                {
                    Vector3 firstNative = bodyInfo.markerPos[first];
                    Vector3 secondNative = bodyInfo.markerPos[second];
                    points[pointIndex++] = new Vector3(firstNative.x, firstNative.z, firstNative.y) / 1000f;
                    points[pointIndex++] = new Vector3(secondNative.x, secondNative.z, secondNative.y) / 1000f;
                }
            }

            lines.positionCount = points.Length;
            lines.SetPositions(points);
            lines.sharedMaterial = lineMaterial;
            lines.startColor = color;
            lines.endColor = color;
            lines.useWorldSpace = false;
            lines.startWidth = 0.005f;
            lines.endWidth = 0.005f;
        }

        bodyObjects.Add(root);
        bodyTransforms.Add(root.transform);
        bodyIds.Add(bodyInfo.id);
    }

    private void DeleteBody(int bodyId)
    {
        for (int index = 0; index < bodyIds.Count; index++)
        {
            if (bodyIds[index] != bodyId)
            {
                continue;
            }

            Destroy(bodyObjects[index]);
            bodyObjects.RemoveAt(index);
            bodyTransforms.RemoveAt(index);
            bodyIds.RemoveAt(index);
            break;
        }

        lock (bodySync)
        {
            reservedBodyIds.Remove(bodyId);
        }
    }

    private void CreateMarker(PrimitiveType type, string objectName, Vector3 position, Transform parent, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(type);
        marker.name = objectName;
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = position;
        marker.transform.localScale = Vector3.one * 0.01f;

        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            colorProperties.SetColor("_Color", color);
            colorProperties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(colorProperties);
        }
    }

    private static Color BodyColor(int[] rgb)
    {
        return rgb != null && rgb.Length >= 3
            ? new Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f)
            : Color.white;
    }

    private void OnDestroy()
    {
        UnregisterCallback(CMPluginAPI.CallbackType.CREATE_BODY, createBodyCallback, createRegistered);
        UnregisterCallback(CMPluginAPI.CallbackType.DELETE_BODY, deleteBodyCallback, deleteRegistered);
        ChingMuCallbackRegistry.Unregister(callbackToken);
        callbackToken = IntPtr.Zero;
        callbackQueue.Clear();

        for (int index = 0; index < bodyObjects.Count; index++)
        {
            if (bodyObjects[index] != null)
            {
                Destroy(bodyObjects[index]);
            }
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
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
            Debug.LogWarning("ChingMu body callback could not be unregistered: " + exception.Message, this);
        }
    }
}
