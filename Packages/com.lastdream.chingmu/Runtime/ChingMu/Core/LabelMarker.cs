using System.Linq;
using UnityEngine;
using System.IO;

public class LabelMarker : MonoBehaviour
{
    public enum PointsType
    {
        Forty = 53,
        Eighteen = 18
    }

    public PointsType pointsType = PointsType.Eighteen;

    Vector3 wPos = new Vector3();
    Quaternion wQuat = new Quaternion();

    CMPluginCommonInterface CMPlugin;

    int[] bodyIds;

    public string Port = "3883";
    string _address;

    string[] fullbodylabels = {
        "M-LBHIPO", "M-LFHIP1", "M-BHIP2", "M-FHIP3", "M-RBHTP4", "M-RFHIP5",
        "M-BTORSO6", "M-FTORSO7", "M-BCHEST8", "M-FCHEST9", "M-THEAD10", "M-FHEAD11",
        "M-BHEAD12", "M-RHEAD13", "M-LHEAD14", "L-BSHOULD15", "L-FSHOULD16", "L-UPARM17",
        "L-LOARM18", "L-LELBOW19", "L-TELBOW20", "L-FHAND21", "L-BHAND22", "L-THUMB23",
        "L-BWIST24", "R-BSHOULD25", "R-FSHOULD26", "R-UPARM27", "R-LOARM28", "R-LELBOW29",
        "R-TELBOW30", "R-FHAND31", "R-BHAND32", "R-THUMB33", "R-BWIST34", "L-THIGH35",
        "L-LEG36", "L-IKNEE37", "L-OKNEE38", "L-OFOOT39"
    };

    string[] labels = {
        "LCIST", "LASIS", "RCIST", "RASIS", "LTROC", "LLEP",
        "LMEP", "LLME", "LMME", "LHM5", "LHM1", "RTROC",
        "RLEP", "RMEP", "RLME", "RMME", "RHM5", "RHM1"
    };

    HumanTracker humanTracker;

    //写入文件
    StreamWriter logWriter;
    string logFilePath;

    private void Start()
    {
        humanTracker = GetComponent<HumanTracker>();

        // 设置日志文件路径
        logFilePath = Path.Combine(Application.dataPath, "LogFile.txt");
        logWriter = new StreamWriter(logFilePath, true);

        if (CMPluginThreadManager.CMPlugin != null)
        {
            CMPlugin = CMPluginThreadManager.CMPlugin;
            _address = CMPlugin.ServerIp + ":" + Port;

            string[] currentLabels = pointsType == PointsType.Forty ? fullbodylabels : labels;
            bodyIds = Enumerable.Range(6300, currentLabels.Length).ToArray();
        }
    }

    private void OnDestroy()
    {
        // 确保在销毁对象时关闭文件
        if (logWriter != null)
        {
            logWriter.Close();
        }
    }

    void FixedUpdate()
    {       
        bool IsInit = CMPlugin == null ? false : true;
        if (IsInit)
        {
            string[] currentLabels = pointsType == PointsType.Forty ? fullbodylabels : labels;
            for (int i = 0; i < bodyIds.Length; i++)
            {
                int bodyId = bodyIds[i];
                CMPluginThreadManager.CMPlugin.GetTrackerPose(bodyId, out wPos, out wQuat);
                transform.localPosition = CMVrpn.CMPos(_address, bodyId);

                string logMessage = $"{currentLabels[i]} == {transform.localPosition}";
                Debug.Log(logMessage);
                logWriter.WriteLine(logMessage);
            }

            if(humanTracker)
            {
                for (int i = 0; i < humanTracker.BonesIndexMapToTransform.Count; i++)
                {
                    if (humanTracker.BonesIndexMapToTransform.ContainsKey(i) && humanTracker.BonesIndexMapToTransform[i] != null)
                    {
                        var boneTransform = humanTracker.BonesIndexMapToTransform[i];
                        string logMessage = $"Bone Index: {i}, Bone Name: {boneTransform.name}, " +
                                            $"Local Position: {boneTransform.localPosition}, " +
                                            $"Local Rotation: {boneTransform.localRotation.eulerAngles}";
                        Debug.Log(logMessage);
                        logWriter.WriteLine(logMessage);
                    }
                }
            }
            logWriter.Flush();
        }
    }
}
