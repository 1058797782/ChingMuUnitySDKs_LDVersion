using System;
using UnityEngine;

public class LabelMarkerTest : MonoBehaviour
{
    public string Port = "3883";
    public bool scanEnabled;
    public int firstBodyId = 6000;
    public int lastBodyId = 6500;
    public int bodiesPerFixedUpdate = 10;
    public bool logDetectedBodies = true;

    private CMPluginCommonInterface plugin;
    private int currentBodyId;

    private void Start()
    {
        plugin = CMPluginThreadManager.CMPlugin;
        currentBodyId = Math.Min(firstBodyId, lastBodyId);
    }

    private void FixedUpdate()
    {
        if (!scanEnabled || plugin == null)
        {
            return;
        }

        int lower = Math.Min(firstBodyId, lastBodyId);
        int upper = Math.Max(firstBodyId, lastBodyId);
        int count = Math.Max(1, bodiesPerFixedUpdate);
        for (int index = 0; index < count; index++)
        {
            Vector3 position;
            Quaternion rotation;
            plugin.GetTrackerPose(currentBodyId, out position, out rotation);
            if (position != Vector3.zero)
            {
                transform.SetPositionAndRotation(position, rotation);
                if (logDetectedBodies)
                {
                    Debug.Log("Body " + currentBodyId + " Position " + position, this);
                }
            }

            currentBodyId++;
            if (currentBodyId > upper)
            {
                currentBodyId = lower;
                scanEnabled = false;
                break;
            }
        }
    }
}
