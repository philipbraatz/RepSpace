using System.Diagnostics;

namespace Doorfail.Distribution.Network;

public record Script(string Name, string Code)
{
    public string[] WaitOn { get; set; } = [];
    public Version Version { get; set; }
    public List<PackageInfo> Packages { get; set; } = [];
    public List<string> Imports { get; set; } = [];
}

public record ScriptGlobals()
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public NodeInfo CurrentNode { get; set; }
    public NetworkStats Stats { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
}

public record NetworkStats
{
    public long Counter { get; set; }
    public Stopwatch Stopwatch { get; set; }
    public DateTimeOffset StartExecution { get; set; } = DateTimeOffset.Now;
	public DateTimeOffset? LastExecution { get; set; }
    public double FPS { get; set; } = 0.0;
}

public enum State
{
    Idle,
    Running,
    Paused,
    Stopped
}


public record PackageInfo(string Name)
{
    public string Version { get; set; } = "*";
    public string Source { get; set; } = "https://api.nuget.org/v3/index.json";
}
