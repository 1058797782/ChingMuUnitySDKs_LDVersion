using System;
using System.Runtime.InteropServices;
using ChingMU;
using NUnit.Framework;
using UnityEngine;

public class ChingMuLiveFrameReaderTests
{
    private IntPtr frame;

    [SetUp]
    public void SetUp()
    {
        int size = Marshal.SizeOf(typeof(CMPluginAPI.tFrame));
        frame = Marshal.AllocHGlobal(size);
        Marshal.Copy(new byte[size], 0, frame, size);
    }

    [TearDown]
    public void TearDown()
    {
        if (frame != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(frame);
            frame = IntPtr.Zero;
        }
    }

    [Test]
    public void BodyLookupUsesBodyIdAndConvertsAxes()
    {
        Marshal.WriteInt32(frame, OffsetOf<CMPluginAPI.tFrame>("bodyNum"), 2);
        IntPtr bodyData = IntPtr.Add(frame, OffsetOf<CMPluginAPI.tFrame>("bodyData"));
        int bodySize = Marshal.SizeOf(typeof(CMPluginAPI.tBody));
        WriteBody(bodyData, 7, new Vector3(10f, 20f, 30f), new Quaternion(1f, 2f, 3f, 4f));
        WriteBody(IntPtr.Add(bodyData, bodySize), 42, new Vector3(1000f, 2000f, 3000f), new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));

        Vector3 position;
        Quaternion rotation;
        bool found = ChingMuLiveFrameReader.TryGetBodyPose(frame, 42, out position, out rotation);

        Assert.That(found, Is.True);
        Assert.That(position, Is.EqualTo(new Vector3(1f, 3f, 2f)));
        Assert.That(rotation, Is.EqualTo(new Quaternion(0.1f, 0.3f, 0.2f, -0.4f)));
    }

    [Test]
    public void HumanLookupUsesHumanIdInsteadOfArrayIndex()
    {
        Marshal.WriteInt32(frame, OffsetOf<CMPluginAPI.tFrame>("humanNum"), 2);
        IntPtr humanData = IntPtr.Add(frame, OffsetOf<CMPluginAPI.tFrame>("humanData"));
        int humanSize = Marshal.SizeOf(typeof(CMPluginAPI.tHuman));
        WriteHuman(humanData, 3, false, Vector3.zero);
        IntPtr target = IntPtr.Add(humanData, humanSize);
        WriteHuman(target, 42, true, new Vector3(1000f, 2000f, 3000f));
        Marshal.WriteInt32(target, OffsetOf<CMPluginAPI.tHuman>("segementNum"), 2);
        IntPtr rotations = IntPtr.Add(target, OffsetOf<CMPluginAPI.tHuman>("segmentQuat"));
        int quaternionSize = Marshal.SizeOf(typeof(Quaternion));
        Marshal.StructureToPtr(new Quaternion(1f, 2f, 3f, 4f), rotations, false);
        Marshal.StructureToPtr(new Quaternion(5f, 6f, 7f, 8f), IntPtr.Add(rotations, quaternionSize), false);
        IntPtr detected = IntPtr.Add(target, OffsetOf<CMPluginAPI.tHuman>("isSegmentDetect"));
        Marshal.WriteByte(detected, 0, 1);
        Marshal.WriteByte(detected, 1, 0);

        Quaternion[] output = new Quaternion[2];
        Vector3 root;
        bool found = ChingMuLiveFrameReader.TryGetHumanPose(frame, 42, null, output, out root, false);

        Assert.That(found, Is.True);
        Assert.That(root, Is.EqualTo(new Vector3(1f, 3f, 2f)));
        Assert.That(output[0], Is.EqualTo(new Quaternion(1f, 3f, 2f, -4f)));
        Assert.That(output[1], Is.EqualTo(Quaternion.identity));
    }

    [Test]
    public void RepeatedBodyReadsDoNotBuildManagedFrameArrays()
    {
        Marshal.WriteInt32(frame, OffsetOf<CMPluginAPI.tFrame>("bodyNum"), 1);
        IntPtr bodyData = IntPtr.Add(frame, OffsetOf<CMPluginAPI.tFrame>("bodyData"));
        WriteBody(bodyData, 1, Vector3.one, Quaternion.identity);
        Vector3 position;
        Quaternion rotation;
        ChingMuLiveFrameReader.TryGetBodyPose(frame, 1, out position, out rotation);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 100; index++)
        {
            ChingMuLiveFrameReader.TryGetBodyPose(frame, 1, out position, out rotation);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.LessThan(4096));
    }

    [Test]
    public void NullFrameReturnsSafeDefaults()
    {
        Vector3 position;
        Quaternion rotation;
        bool found = ChingMuLiveFrameReader.TryGetBodyPose(IntPtr.Zero, 1, out position, out rotation);

        Assert.That(found, Is.False);
        Assert.That(position, Is.EqualTo(Vector3.zero));
        Assert.That(rotation, Is.EqualTo(Quaternion.identity));
    }

    [Test]
    public void UndetectedRetargetKeepsHierarchyOffsets()
    {
        Marshal.WriteInt32(frame, OffsetOf<CMPluginAPI.tFrame>("humanNum"), 1);
        IntPtr human = IntPtr.Add(frame, OffsetOf<CMPluginAPI.tFrame>("humanData"));
        WriteHuman(human, 9, false, Vector3.zero);
        Vector3[] positions = new Vector3[3];
        positions[2] = new Vector3(1f, 2f, 3f);
        Quaternion[] rotations = new Quaternion[3];
        Vector3 root;

        bool found = ChingMuLiveFrameReader.TryGetHumanPose(
            frame,
            9,
            positions,
            rotations,
            out root,
            true);

        Assert.That(found, Is.False);
        Assert.That(positions[2], Is.EqualTo(new Vector3(1f, 2f, 3f)));
        Assert.That(rotations[0], Is.EqualTo(Quaternion.identity));
    }

    private static void WriteBody(IntPtr pointer, int id, Vector3 position, Quaternion rotation)
    {
        Marshal.WriteInt32(pointer, OffsetOf<CMPluginAPI.tBody>("id"), id);
        Marshal.StructureToPtr(position, IntPtr.Add(pointer, OffsetOf<CMPluginAPI.tBody>("pos")), false);
        Marshal.StructureToPtr(rotation, IntPtr.Add(pointer, OffsetOf<CMPluginAPI.tBody>("quat")), false);
    }

    private static void WriteHuman(IntPtr pointer, int id, bool detected, Vector3 rootPosition)
    {
        Marshal.WriteInt32(pointer, OffsetOf<CMPluginAPI.tHuman>("id"), id);
        Marshal.WriteByte(pointer, OffsetOf<CMPluginAPI.tHuman>("isDetect"), detected ? (byte)1 : (byte)0);
        Marshal.StructureToPtr(rootPosition, IntPtr.Add(pointer, OffsetOf<CMPluginAPI.tHuman>("rootPos")), false);
    }

    private static int OffsetOf<T>(string fieldName)
    {
        return Marshal.OffsetOf(typeof(T), fieldName).ToInt32();
    }
}
