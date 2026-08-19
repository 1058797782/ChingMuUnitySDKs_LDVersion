using System;
using ChingMU;
using UnityEngine;

public class VrpnImpl : CMPluginCommonInterface
{
    private const int MaximumSegmentCount = 150;

    private readonly object humanBufferLock = new object();
    private readonly double[] humanAttitude = new double[1000];
    private readonly int[] humanSegmentDetected = new int[MaximumSegmentCount];
    private readonly double[] retargetPosition = new double[3 * MaximumSegmentCount];
    private readonly double[] retargetRotation = new double[4 * MaximumSegmentCount];
    private readonly int[] retargetSegmentDetected = new int[MaximumSegmentCount];

    private string serverAddress = string.Empty;
    private string endpoint = string.Empty;
    private int port;
    private CMPluginAPI.CMServerType serverType;

    public string ServerIp
    {
        get { return serverAddress; }
        set
        {
            serverAddress = value ?? string.Empty;
            endpoint = ChingMuAddress.Build(serverAddress, port);
            serverType = ChingMuAddress.ServerType(serverAddress);
        }
    }

    public int Port
    {
        get { return port; }
        set
        {
            port = value;
            endpoint = ChingMuAddress.Build(serverAddress, port);
        }
    }

    public CMPluginAPI.CMPluginType cMpluginType
    {
        get { return CMPluginAPI.CMPluginType.Vrpn; }
    }

    public CMPluginAPI.CMServerType cMserverType
    {
        get { return serverType; }
        set { serverType = value; }
    }

    public bool ConnectCmServer()
    {
        return endpoint.Length > 0 && CMPluginAPI.CMPluginConnectServer(endpoint);
    }

    public void StartCmThread()
    {
        CMPluginAPI.CMUnityStartExtern();
    }

    public void QuitCmThread()
    {
        CMPluginAPI.CMUnityQuitExtern();
    }

    public void GetTrackerPose(int channel, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        ReadTrackerPose(endpoint, channel, out worldPosition, out worldRotation);
    }

    public void GetTrackerPoseByName(string name, int channel, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        ReadTrackerPose(name, channel, out worldPosition, out worldRotation);
    }

    public bool GetHumanWithoutRetargetPose(int humanId, out Vector3 position, Quaternion[] rotations)
    {
        position = Vector3.zero;
        if (rotations == null || rotations.Length == 0 || endpoint.Length == 0)
        {
            return false;
        }

        lock (humanBufferLock)
        {
            bool detected = CMPluginAPI.CMHumanExtern(
                endpoint,
                humanId,
                Time.frameCount,
                humanAttitude,
                humanSegmentDetected);

            int count = Math.Min(MaximumSegmentCount, rotations.Length);
            if (!detected)
            {
                FillIdentity(rotations, count);
                return false;
            }

            position = new Vector3(
                (float)humanAttitude[0],
                (float)humanAttitude[2],
                (float)humanAttitude[1]) / 1000f;

            for (int index = 0; index < count; index++)
            {
                rotations[index] = humanSegmentDetected[index] == 1
                    ? new Quaternion(
                        (float)humanAttitude[index * 4 + 3],
                        (float)humanAttitude[index * 4 + 5],
                        (float)humanAttitude[index * 4 + 4],
                        -(float)humanAttitude[index * 4 + 6])
                    : Quaternion.identity;
            }

            return true;
        }
    }

    public bool GetHumanWithRetargetPose(int humanId, Vector3[] positions, Quaternion[] rotations)
    {
        if (positions == null || rotations == null || endpoint.Length == 0)
        {
            return false;
        }

        lock (humanBufferLock)
        {
            int timecode = 0;
            bool detected = CMPluginAPI.CMRetargetHumanExternTC(
                endpoint,
                humanId,
                Time.frameCount,
                ref timecode,
                retargetPosition,
                retargetRotation,
                retargetSegmentDetected);

            int count = Math.Min(MaximumSegmentCount, Math.Min(positions.Length, rotations.Length));
            if (!detected)
            {
                FillIdentity(rotations, count);
                return false;
            }

            for (int index = 0; index < count; index++)
            {
                if (retargetSegmentDetected[index] != 1)
                {
                    positions[index] = Vector3.zero;
                    rotations[index] = Quaternion.identity;
                    continue;
                }

                positions[index] = new Vector3(
                    (float)retargetPosition[3 * index],
                    (float)retargetPosition[3 * index + 2],
                    (float)retargetPosition[3 * index + 1]) / 1000f;

                Quaternion rotation = new Quaternion(
                    (float)retargetRotation[index * 4],
                    (float)retargetRotation[index * 4 + 2],
                    (float)retargetRotation[index * 4 + 1],
                    -(float)retargetRotation[index * 4 + 3]);
                rotations[index] = IsFinite(rotation) ? rotation : Quaternion.identity;
            }

            return true;
        }
    }

    private static void ReadTrackerPose(
        string address,
        int channel,
        out Vector3 worldPosition,
        out Quaternion worldRotation)
    {
        worldPosition = Vector3.zero;
        worldRotation = Quaternion.identity;
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        int frame = Time.frameCount;
        worldPosition = new Vector3(
            (float)CMPluginAPI.CMTrackerExtern(address, channel, 0, frame) / 1000f,
            (float)CMPluginAPI.CMTrackerExtern(address, channel, 2, frame) / 1000f,
            (float)CMPluginAPI.CMTrackerExtern(address, channel, 1, frame) / 1000f);
        worldRotation = new Quaternion(
            (float)CMPluginAPI.CMTrackerExtern(address, channel, 3, frame),
            (float)CMPluginAPI.CMTrackerExtern(address, channel, 5, frame),
            (float)CMPluginAPI.CMTrackerExtern(address, channel, 4, frame),
            -(float)CMPluginAPI.CMTrackerExtern(address, channel, 6, frame));
    }

    private static void FillIdentity(Quaternion[] rotations, int count)
    {
        for (int index = 0; index < count; index++)
        {
            rotations[index] = Quaternion.identity;
        }
    }

    private static bool IsFinite(Quaternion value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z) &&
               !float.IsNaN(value.w) && !float.IsInfinity(value.w);
    }
}
