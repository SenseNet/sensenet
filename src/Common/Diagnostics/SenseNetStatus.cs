using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics;

public interface ISenseNetStatus
{
    string Current { get; }
    bool IsRunning { get; set; }
    bool IsInstallerRunning { get; set; }
    void SetStatus(string status);
    string[] GetLog();
}
public class SenseNetStatus : ISenseNetStatus
{
    public static readonly string WaitingForStart = "Waiting for start";
    public static readonly string Starting = "Starting";
    public static readonly string Started = "Started";
    public static readonly string Running = "Running";
    public static readonly string Stopping = "Stopping";
    public static readonly string Stopped = "Stopped";

    private readonly List<string> _history;

    public string Current { get; private set; }
    public bool IsRunning { get; set; }
    public bool IsInstallerRunning { get; set; }

    public SenseNetStatus()
    {
        _history = new List<string>();
        SetStatus("waiting for start");
    }

    public void SetStatus(string status)
    {
        Current = status;
        _history.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.ffff} {status}");
    }

    public string[] GetLog() => _history.ToArray();
}