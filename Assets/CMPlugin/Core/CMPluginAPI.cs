using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


namespace ChingMU 
{

    public static class CMPluginAPI
    {
        public enum CMServerType
        {
            MCServer,
            MCAvatar
        }

        public enum CMPluginType
        {
            LiveStream,
            Vrpn
        }

        public enum CallbackType
        {
            CREATE_BODY = 23,
            CREATE_HUMAN = 25,
            DELETE_HUMAN = 27,
            DELETE_BODY = 29,
            CHANGE_FRAME_RATE = 31,
            CHANGE_TRACK_STATUS = 33,
            FRAME_DATA = 35,
        };

        public enum TrackStatus
        {
            STATUS_LIVE,        // System is live tracking
            STATUS_RECORD,      // System is live recording
            STATUS_REPLAY,      // System is on replaying
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tTimestamp
        {
            public int sec;
            public int usec;
        };

        public struct VrpnHierarchy
        {
            public tTimestamp msg_time;
            public int sensor;
            public int parent;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 127)]
            public string name;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tTimeCode
        {
            public byte hour;
            public byte minute;
            public byte second;
            public ushort frame;
            public ushort subframe;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tHuman
        {
            public int id;
            public byte isDetect;
            public Vector3 rootPos;

            public int segementNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public Quaternion[] segmentQuat;        // local rotation
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public byte[] isSegmentDetect;

            public int markerNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public Vector3[] markerPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public byte[] isMarkerDetect;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct aHumanInfo
        {
            public int humanID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string humanName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] rgb;

            public int segmentNum;
            public Vector3 rootPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public aSegmentInfo[] segmentInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct aSegmentInfo
        {
            public int index;
            public int parentId;
            public int nDofs;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string name;
            public Vector3 posInParent;

            public int markerNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vector3[] markerPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20 * 132)]
            public byte[] markerNames;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct aBodyInfo
        {
            public int id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] rgb;

            public int markerNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public Vector3[] markerPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50 * 132)]
            public byte[] markerNames;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tBody
        {
            public int id;
            public byte isDetect;
            public Vector3 pos;
            public Quaternion quat;

            public int markerNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public Vector3[] markerPos;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tMarker
        {
            public int id;
            public Vector3 pos;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tAnalog
        {
            public int num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public float[] channel;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tButton
        {
            public int num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] buttons;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tFrame
        {
            public uint framecounter;
            public tTimestamp timestamp;
            public tTimeCode timecode;
            public int humanNum;
            public int bodyNum;
            public int markerNum;
            public int buttonNum;
            public int analogNum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public tHuman[] humanData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)]
            public tBody[] bodyData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5000)]
            public tMarker[] markerData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public tButton[] buttonData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public tAnalog[] analogData;
        };


        //VRPN
        [DllImport("CMVrpn")]
        public static extern void CMUnityStartExtern();

        [DllImport("CMVrpn")]
        public static extern bool CMPluginConnectServer(string address);

        [DllImport("CMVrpn")]
        public static extern void CMUnityQuitExtern();

        [DllImport("CMVrpn")]
        public static extern double CMTrackerExtern(string address, int channel, int component, int frameCount, bool lockUpRotation = false);

        [DllImport("CMVrpn")]
        public static extern double CMTrackerNoPredictExtern(string address, int channel, int component, bool lockUpRotation = false);

        [DllImport("CMVrpn")]
        public static extern bool CMHumanExtern(string address, int channel, int frameCount, [In, Out] double[] attitude, [In, Out] int[] segmentIsDetected);

        public delegate void UpdateHierarchyCallback(IntPtr CallBackFun_agrs, VrpnHierarchy CurHierarchy);
        [DllImport("CMVrpn", CallingConvention = CallingConvention.StdCall)]
        public static extern bool CMPluginRegisterUpdateHierarchy(string address, IntPtr CallBackFun_agrs, UpdateHierarchyCallback CallBackGetHierarchy);

        [DllImport("CMVrpn")]
        public static extern bool CMRetargetHumanExternTC(string address, int channel, int frameCount, ref int timecode, [In, Out] double[] position, [In, Out] double[] quaternion, [In, Out] int[] segmentIsDetected);

        public static Vector3 CMPos(string address, int channel)
        {
            return new Vector3(
            (float)CMTrackerExtern(address, channel, 0, Time.frameCount) / 1000f,
            (float)CMTrackerExtern(address, channel, 2, Time.frameCount) / 1000f,
            (float)CMTrackerExtern(address, channel, 1, Time.frameCount) / 1000f);
        }

        /// <summary>
        /// 刚体追踪数据的旋转值Quaternion
        /// Get the Rotation of tracker Body
        /// </summary>
        /// <param name="address">ServerIP，for example:"MCAvatar@192.168.3.178" or "MCAvatar@SH1DT010"</param>
        /// <param name="channel">ID of Body</param>
        /// <param name="lockYRotation">if value is true,the Y axis of rotation  will be lock </param>
        /// <returns></returns>
        public static Quaternion CMQuat(string address, int channel, bool lockYRotation = false)
        {
            return new Quaternion(
            (float)CMTrackerExtern(address, channel, 3, Time.frameCount, lockYRotation),
            (float)CMTrackerExtern(address, channel, 5, Time.frameCount, lockYRotation),
            (float)CMTrackerExtern(address, channel, 4, Time.frameCount, lockYRotation),
            -(float)CMTrackerExtern(address, channel, 6, Time.frameCount, lockYRotation));
        }      

        //LiveStream
        [DllImport("LiveStream")]
        public static extern void StartClientThread();

        [DllImport("LiveStream")]
        private static extern bool SetBindIP(string IP); //local IP

        [DllImport("LiveStream")]
        private static extern void SetBindPort(ushort port); //serverPort

        [DllImport("LiveStream")]
        private static extern bool SetServerIP(string IP);//serverIP

        public static void InitConnectInfoForLiveStream(string LocalIP, string ServerIP)
        {
            SetBindIP(LocalIP);
            SetServerIP(ServerIP);
        }

        [DllImport("LiveStream")]
        public static extern bool ConnectToServer(int timeout);

        [DllImport("LiveStream")]
        public static extern void QuitClientThread();

        [DllImport("LiveStream")]
        public static extern System.IntPtr GetFrameData(bool predict = false, bool localSegRot = true);

        public delegate void callbackDelegate(System.IntPtr userdata, System.IntPtr info);
        [DllImport("LiveStream")]
        public static extern bool RegisterCallback(CallbackType type, callbackDelegate p, System.IntPtr userData);

        [DllImport("LiveStream")]
        public static extern bool UnRegisterCallback(CallbackType type, callbackDelegate p);

        public static void HumanAttitudeLiveStream(tFrame frame, int humanID, out Vector3 humanPos, [In, Out] Quaternion[] rot)
        {
            humanPos = new Vector3((float)frame.humanData[humanID].rootPos.x, (float)frame.humanData[humanID].rootPos.z, (float)frame.humanData[humanID].rootPos.y) / 1000f;

            for (int i = 0; i < 150; i++)
            {
                rot[i] = new Quaternion((float)frame.humanData[humanID].segmentQuat[i].x, (float)frame.humanData[humanID].segmentQuat[i].z, (float)frame.humanData[humanID].segmentQuat[i].y, -(float)frame.humanData[humanID].segmentQuat[i].w);
            }


        }

        public static void HumanRetargetAttitudeLiveStream(tFrame frame, int humanID, out Vector3 humanPos, [In, Out] Quaternion[] rot)
        {
            humanPos = new Vector3((float)frame.humanData[humanID].rootPos.x, (float)frame.humanData[humanID].rootPos.z, (float)frame.humanData[humanID].rootPos.y) / 1000f;

            for (int i = 0; i < 150; i++)
            {
                rot[i] = new Quaternion((float)frame.humanData[humanID].segmentQuat[i].x, (float)frame.humanData[humanID].segmentQuat[i].z, (float)frame.humanData[humanID].segmentQuat[i].y, -(float)frame.humanData[humanID].segmentQuat[i].w);
            }
        }

        //public static void HumanRetargetAttitudeLiveStream(int humanID, [In, Out] Vector3[] LocalPos, [In, Out] Quaternion[] LocalRot)
        //{
        //    tFrame frameData = (tFrame)Marshal.PtrToStructure(GetFrameData(), typeof(tFrame));
        //    Vector3 hipWpos  = new Vector3((float)frameData.humanData[humanID].rootPos.x, (float)frameData.humanData[humanID].rootPos.z, (float)frameData.humanData[humanID].rootPos.y) / 1000f;
        //    LocalPos[1] = hipWpos;
        //    for (int i = 0; i < 150; i++)
        //    {
        //        LocalRot[i] = new Quaternion((float)frameData.humanData[humanID].segmentQuat[i].x, (float)frameData.humanData[humanID].segmentQuat[i].z, (float)frameData.humanData[humanID].segmentQuat[i].y, -(float)frameData.humanData[humanID].segmentQuat[i].w);
        //    }
        //    return frameData.humanData[humanID].isDetect;
        //}


        public static void BodyAttitudeLiveStream(tFrame frame, int bodyID, out Vector3 pos, out Quaternion rot)
        {
            pos = new Vector3((float)frame.bodyData[bodyID].pos.x, (float)frame.bodyData[bodyID].pos.z, (float)frame.bodyData[bodyID].pos.y) / 1000f;

            rot = new Quaternion((float)frame.bodyData[bodyID].quat.x, (float)frame.bodyData[bodyID].quat.z, (float)frame.bodyData[bodyID].quat.y, -(float)frame.bodyData[bodyID].quat.w);
        }


     
    }
}

