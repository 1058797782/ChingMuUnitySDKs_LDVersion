using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Config
{
    private static Config instance;

    private CMUTrackerPreset<int> cmTrackPreset;

    private Config()
    {
        Reload();
    }

    public static Config Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Config();
            }

            return instance;
        }
    }

    public string ServerIP;

    public CMUTrackerPreset<int> CMTrackPreset
    {
        get { return cmTrackPreset; }
    }

    public bool IsLoaded { get; private set; }

    public void Reload()
    {
        CMUTrackerPreset<int> loadedPreset;
        IsLoaded = TryLoadPreset(out loadedPreset);
        cmTrackPreset = loadedPreset ?? new CMUTrackerPreset<int>();
        cmTrackPreset.EnsureCollections();

        string configuredAddress = !string.IsNullOrWhiteSpace(cmTrackPreset.ServerIP)
            ? cmTrackPreset.ServerIP
            : cmTrackPreset.serverIP;
        ServerIP = ChingMuAddress.ApplyConfiguredHost("MCAvatar@", configuredAddress);
        cmTrackPreset.ServerIP = ServerIP;
    }

    public static bool TryReadServerAddress(out string serverAddress)
    {
        CMUTrackerPreset<int> preset;
        if (!TryLoadPreset(out preset) || preset == null)
        {
            serverAddress = string.Empty;
            return false;
        }

        serverAddress = !string.IsNullOrWhiteSpace(preset.ServerIP)
            ? preset.ServerIP
            : preset.serverIP;
        return !string.IsNullOrWhiteSpace(serverAddress);
    }

    public static string ReadJonsFile(string JsonFlieUrl)
    {
        return File.ReadAllText(JsonFlieUrl);
    }

    private static bool TryLoadPreset(out CMUTrackerPreset<int> preset)
    {
        preset = null;
        string[] paths =
        {
            Path.Combine(Application.streamingAssetsPath, "Config.json"),
            Path.Combine(Application.dataPath, "Config.json")
        };

        for (int index = 0; index < paths.Length; index++)
        {
            string path = paths[index];
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                preset = JsonUtility.FromJson<CMUTrackerPreset<int>>(json);
                return preset != null;
            }
            catch (Exception exception)
            {
                Debug.LogError("ChingMu configuration could not be read: " + exception.Message);
                return false;
            }
        }

        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }
}

[Serializable]
public class CMUTrackerPreset<T>
{
    public string ServerIP;
    public string serverIP;
    public List<T> Bodies = new List<T>();
    public List<T> bodiesID = new List<T>();
    public List<T> IMUBodies = new List<T>();
    public List<T> Humans = new List<T>();

    internal void EnsureCollections()
    {
        if (Bodies == null)
        {
            Bodies = new List<T>();
        }
        if (bodiesID == null)
        {
            bodiesID = new List<T>();
        }
        if (Bodies.Count == 0 && bodiesID.Count > 0)
        {
            Bodies.AddRange(bodiesID);
        }
        if (IMUBodies == null)
        {
            IMUBodies = new List<T>();
        }
        if (Humans == null)
        {
            Humans = new List<T>();
        }
    }
}
