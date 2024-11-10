using System.Net.Http.Json;

namespace Doorfail.Distribution.Network;

public class RegionalNode(Uri link, Uri regionalNextNodeUri, Uri? nextNodelink = null, int triggerInterval_Sec = 120) : Node(link, nextNodelink, triggerInterval_Sec)
{
    private readonly Uri _regionalNextNodeUri = regionalNextNodeUri;

    public async Task<int> RegionalScriptCount()
    {
        var totalScriptCount = await GetNextNodeScriptCountAsync();
        Node currentNode = this;

        while (currentNode.NextNodeUri != null && currentNode.NextNodeUri != _info.Link)
        {
            var response = await _httpClient.GetAsync($"{currentNode.NextNodeUri}/ScriptCount");
            response.EnsureSuccessStatusCode();
            var scriptCount = await response.Content.ReadFromJsonAsync<int>();
            totalScriptCount += scriptCount;

            var nextNodeResponse = await _httpClient.GetAsync($"{currentNode.NextNodeUri}/Info");
            nextNodeResponse.EnsureSuccessStatusCode();
            var nextNodeInfo = await nextNodeResponse.Content.ReadFromJsonAsync<NodeInfo>();
            currentNode = new Node(nextNodeInfo.Link);
        }

        return totalScriptCount;
    }

    public async Task TransferScriptsToRegion(Blob blob)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_regionalNextNodeUri}/ReceiveBlob", blob);
        response.EnsureSuccessStatusCode();
    }

    public override async Task<ScriptGlobals?> Run(ScriptGlobals globals, string[]? activateScripts = null)
    {
        // Forward the request to the next node in the loop
        if (NextNodeUri != null)
        {
            var response = await _httpClient.PostAsJsonAsync($"{NextNodeUri}/Run?activateScripts=[{string.Join(',', activateScripts)}]", globals);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ScriptGlobals>();
        }

        return await base.Run(globals);
    }

    public async Task<int> GetNextRegionalNodeScriptCount()
    {
        var response = await _httpClient.GetAsync($"{_regionalNextNodeUri}/ScriptCount");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    private async void RequestRegionalWork(object state)
    {
        if (NextNodeUri == null || State == NodeState.Running)
            return;

        var nextNodeScriptCount = await GetNextNodeScriptCountAsync();
        var regionalScriptCount = await GetNextRegionalNodeScriptCount();

        if (_scripts.Count > regionalScriptCount * 1.25 && _scripts.Count / 2 > 0)
        {
            State = NodeState.Full;
            Console.WriteLine($"{Id} Sending {_scripts.Count / 2} of {_scripts.Count} scripts | Regional node had {regionalScriptCount} scripts");

            var scripts = _scripts.Take(_scripts.Count / 2);
            var balanceBlob = new Blob(scripts
                    .Select(s => (Guid.NewGuid(), s)).ToDictionary(k => k.Item1, v =>
                        Compression.Compress(
                            System.Text.Json.JsonSerializer.Serialize(v.s),
                            v.Item1
                )));

            var response = await _httpClient.PostAsJsonAsync($"{_regionalNextNodeUri}/BalanceScripts", balanceBlob);
            response.EnsureSuccessStatusCode();

            var removeCount = _scripts.RemoveAll(s => scripts.Contains(s));
        }
        else
        {
            Console.WriteLine($"{Id} stable with {_scripts.Count} scripts | Regional node has {regionalScriptCount} scripts");
            State = NodeState.Idle;
        }
    }

    public async Task BalanceRegionalScripts(Blob blob)
    {
        var scriptData = blob.CompressedData.Select(s => blob.ViewSilhouette(s.Key));

        _scripts.AddRange(scriptData);
        Console.WriteLine($"{Id} Added {scriptData.Count()} Scripts | Total {_scripts.Count}");
    }
}
