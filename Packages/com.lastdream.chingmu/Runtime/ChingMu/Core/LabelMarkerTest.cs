using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LabelMarkerTest : MonoBehaviour
{
    Vector3 wPos = new Vector3();
    Quaternion wQuat = new Quaternion();

    CMPluginCommonInterface CMPlugin;

    int[] bodyIds = Enumerable.Range(6000, 501).ToArray();

    public string Port = "3883";
    string _address;

    private void Start()
    {
        if (CMPluginThreadManager.CMPlugin != null)
        {
            CMPlugin = CMPluginThreadManager.CMPlugin;
            _address = CMPlugin.ServerIp + ":" + Port;
        }
    }

    void FixedUpdate()
    {
        bool IsInit = CMPlugin == null ? false : true;
        if (IsInit)
        {
            foreach (int bodyId in bodyIds)
            {
                CMPluginThreadManager.CMPlugin.GetTrackerPose(bodyId, out wPos, out wQuat);
                transform.localPosition = CMVrpn.CMPos(_address, bodyId);

                Debug.Log($"Body ID: {bodyId}, Position: {transform.localPosition}");

                if (transform.localPosition != Vector3.zero)
                {
                    Debug.Log($"Body ID: {bodyId}, Position: {transform.localPosition}");
                }
            }
        }
    }
}
