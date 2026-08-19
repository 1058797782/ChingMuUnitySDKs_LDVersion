using System;
using System.Runtime.InteropServices;
using ChingMU;
using UnityEngine;

internal sealed class ChingMuLiveFrameReader
{
    private const int MaxHumans = 100;
    private const int MaxBodies = 1000;
    private const int MaxSegments = 200;

    private static readonly int FrameHumanCountOffset = OffsetOf<CMPluginAPI.tFrame>("humanNum");
    private static readonly int FrameBodyCountOffset = OffsetOf<CMPluginAPI.tFrame>("bodyNum");
    private static readonly int FrameHumanDataOffset = OffsetOf<CMPluginAPI.tFrame>("humanData");
    private static readonly int FrameBodyDataOffset = OffsetOf<CMPluginAPI.tFrame>("bodyData");
    private static readonly int HumanSize = Marshal.SizeOf(typeof(CMPluginAPI.tHuman));
    private static readonly int HumanIdOffset = OffsetOf<CMPluginAPI.tHuman>("id");
    private static readonly int HumanDetectedOffset = OffsetOf<CMPluginAPI.tHuman>("isDetect");
    private static readonly int HumanRootPositionOffset = OffsetOf<CMPluginAPI.tHuman>("rootPos");
    private static readonly int HumanSegmentCountOffset = OffsetOf<CMPluginAPI.tHuman>("segementNum");
    private static readonly int HumanSegmentRotationOffset = OffsetOf<CMPluginAPI.tHuman>("segmentQuat");
    private static readonly int HumanSegmentDetectedOffset = OffsetOf<CMPluginAPI.tHuman>("isSegmentDetect");
    private static readonly int BodySize = Marshal.SizeOf(typeof(CMPluginAPI.tBody));
    private static readonly int BodyIdOffset = OffsetOf<CMPluginAPI.tBody>("id");
    private static readonly int BodyPositionOffset = OffsetOf<CMPluginAPI.tBody>("pos");
    private static readonly int BodyRotationOffset = OffsetOf<CMPluginAPI.tBody>("quat");
    private static readonly int QuaternionSize = Marshal.SizeOf(typeof(Quaternion));

    private IntPtr framePointer;
    private int unityFrame = -1;

    internal bool TryGetBodyPose(int bodyId, out Vector3 position, out Quaternion rotation)
    {
        return TryGetBodyPose(CurrentFrame(), bodyId, out position, out rotation);
    }

    internal static bool TryGetBodyPose(
        IntPtr frame,
        int bodyId,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (frame == IntPtr.Zero)
        {
            return false;
        }

        int count = ClampCount(Marshal.ReadInt32(frame, FrameBodyCountOffset), MaxBodies);
        IntPtr data = IntPtr.Add(frame, FrameBodyDataOffset);
        for (int index = 0; index < count; index++)
        {
            IntPtr body = IntPtr.Add(data, index * BodySize);
            if (Marshal.ReadInt32(body, BodyIdOffset) != bodyId)
            {
                continue;
            }

            Vector3 nativePosition = ReadVector3(body, BodyPositionOffset);
            Quaternion nativeRotation = ReadQuaternion(body, BodyRotationOffset);
            position = new Vector3(nativePosition.x, nativePosition.z, nativePosition.y) / 1000f;
            rotation = new Quaternion(nativeRotation.x, nativeRotation.z, nativeRotation.y, -nativeRotation.w);
            return true;
        }

        return false;
    }

    internal bool TryGetHumanPose(
        int humanId,
        Vector3[] positions,
        Quaternion[] rotations,
        out Vector3 rootPosition,
        bool includeSegmentPositions)
    {
        return TryGetHumanPose(
            CurrentFrame(),
            humanId,
            positions,
            rotations,
            out rootPosition,
            includeSegmentPositions);
    }

    internal static bool TryGetHumanPose(
        IntPtr frame,
        int humanId,
        Vector3[] positions,
        Quaternion[] rotations,
        out Vector3 rootPosition,
        bool includeSegmentPositions)
    {
        rootPosition = Vector3.zero;
        if (rotations == null)
        {
            return false;
        }

        IntPtr human;
        if (!TryFindHuman(frame, humanId, out human))
        {
            return false;
        }

        bool detected = Marshal.ReadByte(human, HumanDetectedOffset) == 1;
        if (!detected)
        {
            ClearOutputs(positions, rotations, false);
            return false;
        }

        Vector3 nativeRoot = ReadVector3(human, HumanRootPositionOffset);
        rootPosition = new Vector3(nativeRoot.x, nativeRoot.z, nativeRoot.y) / 1000f;
        if (includeSegmentPositions && positions != null && positions.Length > 1)
        {
            positions[1] = rootPosition;
        }

        int segmentCount = includeSegmentPositions
            ? MaxSegments
            : ClampCount(Marshal.ReadInt32(human, HumanSegmentCountOffset), MaxSegments);
        segmentCount = Math.Min(segmentCount, rotations.Length);
        IntPtr rotationData = IntPtr.Add(human, HumanSegmentRotationOffset);
        IntPtr detectedData = IntPtr.Add(human, HumanSegmentDetectedOffset);

        for (int index = 0; index < segmentCount; index++)
        {
            bool segmentDetected = includeSegmentPositions || Marshal.ReadByte(detectedData, index) == 1;
            if (!segmentDetected)
            {
                rotations[index] = Quaternion.identity;
                continue;
            }

            Quaternion nativeRotation = Marshal.PtrToStructure<Quaternion>(
                IntPtr.Add(rotationData, index * QuaternionSize));
            rotations[index] = new Quaternion(
                nativeRotation.x,
                nativeRotation.z,
                nativeRotation.y,
                -nativeRotation.w);
        }

        for (int index = segmentCount; index < rotations.Length; index++)
        {
            rotations[index] = Quaternion.identity;
        }

        return true;
    }

    internal void Invalidate()
    {
        unityFrame = -1;
        framePointer = IntPtr.Zero;
    }

    private IntPtr CurrentFrame()
    {
        int currentFrame = Time.frameCount;
        if (unityFrame != currentFrame || framePointer == IntPtr.Zero)
        {
            framePointer = CMPluginAPI.GetFrameData();
            unityFrame = currentFrame;
        }

        return framePointer;
    }

    private static bool TryFindHuman(IntPtr frame, int humanId, out IntPtr human)
    {
        human = IntPtr.Zero;
        if (frame == IntPtr.Zero)
        {
            return false;
        }

        int count = ClampCount(Marshal.ReadInt32(frame, FrameHumanCountOffset), MaxHumans);
        IntPtr data = IntPtr.Add(frame, FrameHumanDataOffset);
        for (int index = 0; index < count; index++)
        {
            IntPtr candidate = IntPtr.Add(data, index * HumanSize);
            if (Marshal.ReadInt32(candidate, HumanIdOffset) == humanId)
            {
                human = candidate;
                return true;
            }
        }

        return false;
    }

    private static int ClampCount(int value, int maximum)
    {
        return value < 0 ? 0 : Math.Min(value, maximum);
    }

    private static int OffsetOf<T>(string fieldName)
    {
        return Marshal.OffsetOf(typeof(T), fieldName).ToInt32();
    }

    private static Vector3 ReadVector3(IntPtr pointer, int offset)
    {
        return Marshal.PtrToStructure<Vector3>(IntPtr.Add(pointer, offset));
    }

    private static Quaternion ReadQuaternion(IntPtr pointer, int offset)
    {
        return Marshal.PtrToStructure<Quaternion>(IntPtr.Add(pointer, offset));
    }

    private static void ClearOutputs(Vector3[] positions, Quaternion[] rotations, bool clearPositions)
    {
        if (clearPositions && positions != null)
        {
            Array.Clear(positions, 0, positions.Length);
        }

        for (int index = 0; index < rotations.Length; index++)
        {
            rotations[index] = Quaternion.identity;
        }
    }
}
