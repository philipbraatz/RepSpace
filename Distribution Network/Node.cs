using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Node_Network_Api.Controllers;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace Doorfail.Distribution.Network;

public struct NodeInfo
{
    public Guid Id { get; set; }
    public Uri Link { get; set; }
    public RSAParameters PublicKey { get; set; }
    public Version NodeVersion { get; set; }
}

public enum NodeState
{
    Idle,
    Running,
    Full
}

public class Node : INodeRequests
{
    protected readonly NodeInfo _info;
    public NodeInfo Info() => _info;
    public Guid Id => _info.Id;

    // Node state based on silhouette data
    protected List<Script> _scripts = new();
    public int ScriptCount() => _scripts.Count;

    // Reference to the next node (for propagation)
    public Uri? NextNodeUri { get; private set; }
    public void NextNode(Uri newNextNodeUri) => NextNodeUri = newNextNodeUri;

    protected readonly Timer _workRequestTimer;
    protected readonly HttpClient _httpClient;

    protected readonly HashSet<Guid> _processedScriptGlobals = new();

    public NodeState State { get; protected set; } = NodeState.Idle;

    public Node(Uri link, Uri? nextNodelink = null, int triggerInterval_Sec = 15)
    {
        _info.Id = Guid.NewGuid();
        _info.Link = link;
        _httpClient = new HttpClient();
        _workRequestTimer = new Timer(RequestWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(triggerInterval_Sec * (0.5 + Random.Shared.NextDouble())));
        NextNodeUri = nextNodelink ?? link; // Default to itself
    }

    public void PrimeScript(params Script[] scripts) => _scripts.AddRange(scripts);

    public void PurgeScript(params string[] scriptNames) => _scripts.RemoveAll(s => scriptNames.Contains(s.Name));

    private async void RequestWork(object state)
    {
        if (NextNodeUri == null || State == NodeState.Running)
            return;

        var nextNodeScriptCount = await GetNextNodeScriptCountAsync();
        if (_scripts.Count > nextNodeScriptCount * 1.25 && _scripts.Count / 2 > 0)
        {
            State = NodeState.Full;
            Console.WriteLine($"{Id} Sending {_scripts.Count / 2} of {_scripts.Count} scripts | Next node had {nextNodeScriptCount} scripts");

            var scripts = _scripts.Take(_scripts.Count / 2);
            var balanceBlob = new Blob(scripts
                    .Select(s => (Guid.NewGuid(), s)).
                    ToDictionary(k => k.Item1, v =>
                        Compression.Compress(
                            System.Text.Json.JsonSerializer.Serialize(v.s),
                            v.Item1
                )));

            var test = System.Text.Json.JsonSerializer.Serialize(balanceBlob);
            var response = await _httpClient.PostAsJsonAsync($"{NextNodeUri}/BalanceScripts", balanceBlob);
            response.EnsureSuccessStatusCode();

            var removeCount = _scripts.RemoveAll(s => scripts.Contains(s));
        }
        else
        {
            Console.WriteLine($"{Id} stable with {_scripts.Count} scripts | Next node has {nextNodeScriptCount} scripts");
            State = NodeState.Idle;
        }
    }

    public async Task ReceiveBlob(Blob blob)
    {
        Console.WriteLine($"{Id} Received {blob.CompressedData.Count} Scripts");
        _scripts.AddRange(blob.CompressedData.Select(cd => blob.ConsomeSilhouette(cd.Key, cd.Value)));
    }

    public async Task BalanceScripts(Blob blob)
    {
        var scriptData = blob.CompressedData.Select(s => blob.ViewSilhouette(s.Key));

        _scripts.AddRange(scriptData);
        Console.WriteLine($"{Id} Added {scriptData.Count()} Scripts | Total {_scripts.Count}");
    }

    protected async Task<int> GetNextNodeScriptCountAsync()
    {
        var response = await _httpClient.GetAsync($"{NextNodeUri}/ScriptCount");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public virtual async Task<ScriptGlobals?> Run(ScriptGlobals globals, string[]? activateScripts = null)
    {
        try
        {
            // Check if the current node has already processed this ScriptGlobals
            if (_processedScriptGlobals.Contains(globals.Id))
                return null;

            // Mark the ScriptGlobals as processed
            _processedScriptGlobals.Add(globals.Id);

            Console.WriteLine($"{Id} Running Scripts");

            // Compile and execute the code in _scripts with the options, globals, and cancellation token
            var results = new List<string>();
            foreach (var script in _scripts.Where(w => !activateScripts.Any() || activateScripts.Contains(w.Name)))
            {
                var scriptOptions = ScriptOptions.Default
                    .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(a => !string.IsNullOrEmpty(a.Location))) // Add default assemblies with location
                    .WithReferences(await Task.WhenAll(script.Packages.Select(PackageManager.LoadNuGetAssembly))) // Load packages
                    .WithImports(_scripts.SelectMany(s => s.Imports)) // Add imports
                    .WithFilePath($"{_scripts.First().Name}.csx"); // Optional: set a file path for debugging

                // Create a cancellation token
                using var cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                var result = await CSharpScript.EvaluateAsync<string>(script.Code, scriptOptions, globals, typeof(ScriptGlobals), cancellationToken);
                results.Add(result);
            }

            Console.WriteLine($"{Id} Completed Scripts");

            // Post ScriptGlobals to the next node
            if (NextNodeUri != null)
            {
                var response = await _httpClient.PostAsJsonAsync($"{NextNodeUri}/providecontribution", globals);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            // Handle compilation/execution errors
            Console.WriteLine($"{_info.Link} encountered an error in code execution: {ex.Message}\r\n{ex.InnerException?.Message}\r\n{ex.StackTrace}");
        }

        return globals;
    }

    public void UpdateGlobals(string key, object value)
    {
        lock (_globalsLock)
        {
            _globalsData[key] = value;
            Monitor.PulseAll(_globalsLock);
        }
    }

    private readonly object _globalsLock = new();
    private readonly Dictionary<string, object> _globalsData = new();
}
