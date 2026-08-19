using System;
using System.IO;
using UnityEngine;

public class LabelMarker : MonoBehaviour
{
    public enum PointsType
    {
        Forty = 53,
        Eighteen = 18
    }

    public PointsType pointsType = PointsType.Eighteen;
    public string Port = "3883";
    public bool captureEnabled;
    public bool logToConsole;
    public bool writeToFile;
    public float sampleInterval = 0.25f;
    public string logFileName = "ChingMuMarkers.log";

    private static readonly string[] FullBodyLabels =
    {
        "M-LBHIPO", "M-LFHIP1", "M-BHIP2", "M-FHIP3", "M-RBHTP4", "M-RFHIP5",
        "M-BTORSO6", "M-FTORSO7", "M-BCHEST8", "M-FCHEST9", "M-THEAD10", "M-FHEAD11",
        "M-BHEAD12", "M-RHEAD13", "M-LHEAD14", "L-BSHOULD15", "L-FSHOULD16", "L-UPARM17",
        "L-LOARM18", "L-LELBOW19", "L-TELBOW20", "L-FHAND21", "L-BHAND22", "L-THUMB23",
        "L-BWIST24", "R-BSHOULD25", "R-FSHOULD26", "R-UPARM27", "R-LOARM28", "R-LELBOW29",
        "R-TELBOW30", "R-FHAND31", "R-BHAND32", "R-THUMB33", "R-BWIST34", "L-THIGH35",
        "L-LEG36", "L-IKNEE37", "L-OKNEE38", "L-OFOOT39"
    };

    private static readonly string[] Labels =
    {
        "LCIST", "LASIS", "RCIST", "RASIS", "LTROC", "LLEP", "LMEP", "LLME", "LMME",
        "LHM5", "LHM1", "RTROC", "RLEP", "RMEP", "RLME", "RMME", "RHM5", "RHM1"
    };

    private CMPluginCommonInterface plugin;
    private HumanTracker humanTracker;
    private int[] bodyIds;
    private StreamWriter logWriter;
    private float nextSampleTime;

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        humanTracker = GetComponent<HumanTracker>();
        string[] labels = CurrentLabels;
        bodyIds = new int[labels.Length];
        for (int index = 0; index < bodyIds.Length; index++)
        {
            bodyIds[index] = 6300 + index;
        }

        if (captureEnabled && writeToFile)
        {
            string safeName = Path.GetFileName(logFileName);
            string path = Path.Combine(Application.persistentDataPath, safeName);
            logWriter = new StreamWriter(path, true);
        }
    }

    private void FixedUpdate()
    {
        if (!captureEnabled || plugin == null || Time.unscaledTime < nextSampleTime)
        {
            return;
        }

        nextSampleTime = Time.unscaledTime + Math.Max(0.02f, sampleInterval);
        string[] labels = CurrentLabels;
        for (int index = 0; index < bodyIds.Length; index++)
        {
            Vector3 position;
            Quaternion rotation;
            plugin.GetTrackerPose(bodyIds[index], out position, out rotation);
            WriteMessage(labels[index] + " == " + position);
        }

        if (humanTracker == null || humanTracker.BonesIndexMapToTransform == null)
        {
            return;
        }

        foreach (var pair in humanTracker.BonesIndexMapToTransform)
        {
            Transform bone = pair.Value;
            if (bone != null)
            {
                WriteMessage("Bone " + pair.Key + " " + bone.name + " Position " +
                             bone.localPosition + " Rotation " + bone.localRotation.eulerAngles);
            }
        }
    }

    private string[] CurrentLabels
    {
        get { return pointsType == PointsType.Forty ? FullBodyLabels : Labels; }
    }

    private void WriteMessage(string message)
    {
        if (logToConsole)
        {
            Debug.Log(message, this);
        }
        if (logWriter != null)
        {
            logWriter.WriteLine(message);
        }
    }

    private void OnDestroy()
    {
        if (logWriter != null)
        {
            logWriter.Dispose();
            logWriter = null;
        }
    }
}
