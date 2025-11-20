// Assets/Scripts/Utils/ExperimentLogger.cs
using UnityEngine;
using System.IO;
using System;

public class ExperimentLogger : MonoBehaviour
{
    [SerializeField] string fileName = "experiment_log.csv";
    string path;

    void Awake()
    {
        var dir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(dir);
        path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
        File.WriteAllText(path, "ts_ms,event,value\n");
    }

    public void Log(string evt, string val = "")
    {
        var line = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},{evt},{val}\n";
        File.AppendAllText(path, line);
    }
}

