using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration;

public class ConnectionStringOptions
{
    public string Repository { get; set; }

    private string _security;
    public string Security
    {
        get => _security ?? Repository;
        set => _security = value;
    }

    private string _signalR;
    public string SignalR
    {
        get => _signalR ?? Repository;
        set => _signalR = value;
    }

    public IDictionary<string, string> AllConnectionStrings { get; set; }

}