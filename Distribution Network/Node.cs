using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Node_Network_Api.Controllers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace Doorfail.Distribution.Network;

public struct NodeInfo
{
    public Guid Id { get; set; }
    public Uri Link { get; set; }
    public RSAParameters PublicKey { get; set; }
    public Version NodeVersion { get; set; }
}

public class Node : INodeRequests
{
    private readonly NodeInfo _info;
    public NodeInfo Info() => _info;
    public Guid Id => _info.Id;

    // Node state based on silhouette data
    private List<Script> _scripts = new();
    public int ScriptCount() => _scripts.Count;

    // Reference to the next node (for propagation)
    public Uri? NextNodeUri { get; private set; }
    public void NextNode(Uri newNextNodeUri) => NextNodeUri = newNextNodeUri;

    private readonly Timer _workRequestTimer;
    private readonly HttpClient _httpClient;

    private readonly HashSet<Guid> _processedScriptGlobals = new();

    public Node(Uri link, Uri? nextNodelink = null)
    {
        _info.Id = Guid.NewGuid();
        _info.Link = link;
        _httpClient = new HttpClient();
        _workRequestTimer = new Timer(RequestWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1 + Random.Shared.NextDouble()));
        NextNodeUri = nextNodelink ?? link; // Default to itself

    }

    public void PrimeScript(params Script[] scripts) => _scripts.AddRange(scripts);

    public void PurgeScript(params string[] scriptNames) => _scripts.RemoveAll(s => scriptNames.Contains(s.Name));

    private void RequestWork(object state)
    {
        if (NextNodeUri == null)
            return;

        var requestBlob = new Blob([]);
        SendBlobAsync(requestBlob, NextNodeUri).RunSynchronously();
    }

    public async Task SendBlobAsync(Blob blob, Uri uri)
    {
        Console.WriteLine($"{Id} sending blob to {uri}...");

        var response = await _httpClient.PostAsJsonAsync($"{uri}/api/node/receiveblob", blob);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReceiveBlob(Blob blob)
    {
        if (!blob._compressedData.Any() && NextNodeUri != null)
        {
            await BalanceScriptsAsync(blob);
        }
        else
        {
            _scripts.AddRange(blob._compressedData.Select(cd => blob.ConsomeSilhouette(cd.Key, cd.Value)));
        }
    }

    private async Task BalanceScriptsAsync(Blob blob)
    {
        if (NextNodeUri != null && _scripts.Count > await GetNextNodeScriptCountAsync())
        {
            int scriptsToTransfer = (_scripts.Count - await GetNextNodeScriptCountAsync()) / 2;
            var scriptsToSend = _scripts.Take(scriptsToTransfer).ToList();
            var scriptData = scriptsToSend.ToDictionary(s => Guid.NewGuid(), s => Encoding.UTF8.GetBytes(s.Code));
            var transferBlob = new Blob(scriptData);

            _scripts.RemoveAll(s => scriptsToSend.Contains(s));
            await SendBlobAsync(transferBlob, NextNodeUri);
        }
    }

    private async Task<int> GetNextNodeScriptCountAsync()
    {
        var response = await _httpClient.GetAsync($"{NextNodeUri}/api/node/getscriptcount");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<ScriptGlobals?> Run(ScriptGlobals globals)
    {
        try
        {
            // Check if the current node has already processed this ScriptGlobals
            if (_processedScriptGlobals.Contains(globals.Id))
            {
                return null;
            }

            // Mark the ScriptGlobals as processed
            _processedScriptGlobals.Add(globals.Id);

            var scriptOptions = ScriptOptions.Default
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(a => !string.IsNullOrEmpty(a.Location))) // Add default assemblies with location
                .WithReferences(await Task.WhenAll(_scripts.SelectMany(s => s.Packages).Select(PackageManager.LoadNuGetAssembly))) // Load packages
                .WithImports(_scripts.SelectMany(s => s.Imports)) // Add imports
                .WithFilePath($"{_scripts.First().Name}.csx"); // Optional: set a file path for debugging

            // Create a cancellation token
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Compile and execute the code in _scripts with the options, globals, and cancellation token
            var results = new List<string>();
            foreach (var script in _scripts)
            {
                var result = await CSharpScript.EvaluateAsync<string>(script.Code, scriptOptions, globals, typeof(ScriptGlobals), cancellationToken);
                results.Add(result);
            }

            // Post ScriptGlobals to the next node
            if (NextNodeUri != null)
            {
                var response = await _httpClient.PostAsJsonAsync($"{NextNodeUri}/api/node/providecontribution", globals);
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

    private readonly object _globalsLock = new object();
    private readonly Dictionary<string, object> _globalsData = new();

}
