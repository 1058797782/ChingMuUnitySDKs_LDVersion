using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformData
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public TransformData(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

public class VRPNSetup : MonoBehaviour
{
    public delegate void LogHandler(string message);
    public static event LogHandler OnLog;

    public string Port = "3883";
    public int maximumBodyId = 99;
    public int bodiesPerFrame = 10;
    public bool logContinuously;
    public float logInterval = 0.25f;

    private readonly Dictionary<int, TransformData> trackerTransforms = new Dictionary<int, TransformData>();
    private readonly List<int> detectedBodyIds = new List<int>();
    private CMPluginCommonInterface plugin;
    private Coroutine scanRoutine;
    private float nextLogTime;
    private string address;

    private void Start()
    {
        plugin = VRPNSetupManager.CMPlugin ?? CMPluginThreadManager.CMPlugin;
        if (plugin != null)
        {
            int configuredPort;
            int.TryParse(Port, out configuredPort);
            address = ChingMuAddress.Build(plugin.ServerIp, configuredPort);
            scanRoutine = StartCoroutine(ScanBodies());
        }
    }

    private IEnumerator ScanBodies()
    {
        trackerTransforms.Clear();
        detectedBodyIds.Clear();
        int batchSize = Math.Max(1, bodiesPerFrame);
        int processed = 0;

        for (int bodyId = 0; bodyId <= Math.Max(0, maximumBodyId); bodyId++)
        {
            Vector3 position;
            Quaternion rotation;
            plugin.GetTrackerPose(bodyId, out position, out rotation);
            bool detected = plugin.cMpluginType == ChingMU.CMPluginAPI.CMPluginType.Vrpn
                ? CMVrpn.CMTrackerIsDetected(address, bodyId)
                : position != Vector3.zero || rotation != Quaternion.identity;
            if (detected)
            {
                trackerTransforms.Add(bodyId, new TransformData(position, rotation));
                detectedBodyIds.Add(bodyId);
            }

            processed++;
            if (processed >= batchSize)
            {
                processed = 0;
                yield return null;
            }
        }

        scanRoutine = null;
    }

    private void FixedUpdate()
    {
        if (!logContinuously || plugin == null || Time.unscaledTime < nextLogTime)
        {
            return;
        }

        nextLogTime = Time.unscaledTime + Math.Max(0.02f, logInterval);
        for (int index = 0; index < detectedBodyIds.Count; index++)
        {
            int bodyId = detectedBodyIds[index];
            Vector3 position;
            Quaternion rotation;
            plugin.GetTrackerPose(bodyId, out position, out rotation);
            TransformData data = trackerTransforms[bodyId];
            data.Position = position;
            data.Rotation = rotation;

            string message = "BodyCount = [" + detectedBodyIds.Count + "] | BodySensor = [" + bodyId +
                             "] | Position = [" + position + "] | Rotation = [" + rotation + "]";
            OnLog?.Invoke(message);
            Debug.Log(message, this);
        }
    }

    public void Rescan()
    {
        if (plugin == null)
        {
            return;
        }

        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
        }
        scanRoutine = StartCoroutine(ScanBodies());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        OnLog = null;
    }
}
