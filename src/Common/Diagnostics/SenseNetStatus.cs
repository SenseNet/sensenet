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
    private readonly object _sync = new();
    private string _current;
    private bool _isRunning;
    private bool _isInstallerRunning;

    /// <summary>Signals an in-memory status change. Subscribers must not perform blocking work.</summary>
    public event EventHandler Changed;
    public string Current { get { lock (_sync) return _current; } }
    public bool IsRunning
    {
        get { lock (_sync) return _isRunning; }
        set { lock (_sync) _isRunning = value; Changed?.Invoke(this, EventArgs.Empty); }
    }
    public bool IsInstallerRunning
    {
        get { lock (_sync) return _isInstallerRunning; }
        set { lock (_sync) _isInstallerRunning = value; }
    }

    public SenseNetStatus()
    {
        _history = new List<string>();
        SetStatus("waiting for start");
    }

    public void SetStatus(string status)
    {
        lock (_sync)
        {
            _current = status;
            _history.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.ffff} {status}");
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string[] GetLog() { lock (_sync) return _history.ToArray(); }

    /// <summary>Copies the current state and history consistently for background diagnostics.</summary>
    public SenseNetStatusSnapshot GetSnapshot()
    {
        lock (_sync) return new SenseNetStatusSnapshot(_isRunning, _current, _history.ToArray());
    }
}

public sealed record SenseNetStatusSnapshot(bool Running, string Status, string[] History);
