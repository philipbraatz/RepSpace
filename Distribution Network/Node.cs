using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Doorfail.Distribution.Network;

public struct NodeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public RSAParameters PublicKey { get; set; }
    public Version ScriptVersion { get; set; }
}

public class Node
{
    public readonly NodeInfo Info;
    public Guid Id => Info.Id;
    private readonly RSA _privateKey;
    private RSAParameters PublicKey => Info.PublicKey;

    // Node state based on silhouette data
    private Script _state = new("Initial Script", "return \"Initial state.\";");

    // Reference to the next node (for propagation)
    public Node NextNode { get; set; }
    public Node(string name = null)
    {
        Info.Id = Guid.NewGuid();
        _privateKey = RSA.Create();
        Info.PublicKey = _privateKey.ExportParameters(false);
        Info.Name = name;
    }

    public RSAParameters GetPublicKey() => PublicKey;

    public void SendBlob(Dictionary<RSAParameters, Script> data)
    {
        // Create and send the blob to the next node
        var blob = Blob.CreateCompressedBlob(data);
        SendBlob(blob, this.PublicKey);
    }

    private void SendBlob(Dictionary<RSAParameters, Script> data, RSAParameters? intialSender = null)
    {
        // Create and send the blob to the next node
        var blob = Blob.CreateCompressedBlob(data);
        Console.WriteLine($"{Id} sending blob to the next node...");

        // Only send to the next node if it exists
        NextNode.ReceiveBlob(blob, intialSender ?? this.PublicKey);
    }

    private void SendBlob(Blob blob, RSAParameters intialSender)
    {
        Console.WriteLine($"{Id} sending blob to the next node...");

        // Only send to the next node if it exists
        NextNode.ReceiveBlob(blob, intialSender);
    }

    public void ReceiveBlob(Blob blob, RSAParameters initialSender)
    {
        var silhouetteData = blob.ViewSilhouette(PublicKey, _privateKey);

        if (silhouetteData != null)
        {
            // Update node state with silhouette data as code
            _state = silhouetteData;
            Console.WriteLine($"{Id} updated: {_state.Code}");

        }
        else
        {
            Console.WriteLine($"{Id} could not decrypt silhouette for its public key.");
        }

        // Propagate the blob to the next node
        // Prevent sending back to the sender to avoid loops
        if (!NextNode?.PublicKey.Equals(initialSender) ?? false)
        {
            NextNode.SendBlob(blob, initialSender);
        }
    }

    public async Task<string?> AggregateAsync(RSAParameters? publicKey = null, bool start = false)
    {
        if (!start && Info.PublicKey.Equals(publicKey))
        {
            Console.WriteLine("Completed Aggregation");
            return null;
        }

        Console.WriteLine($"{Info.Name} initiating aggregation request...");

        var globals = new ScriptGlobals
        {
            Node = this.Info,
            Data = new Dictionary<string, object>(),
            Stats = new NetworkStats
            {
                Counter = 0,
                Stopwatch = new Stopwatch(),
                StartExecution = DateTimeOffset.Now,
                LastExecution = null,
                FPS = 0,
            },
        };

        var tasks = new List<Task<string>>();
        var nodes = new List<Node> { this };

        // Collect all nodes in the network
        var currentNode = this.NextNode;
        while (currentNode != null && currentNode != this)
        {
            nodes.Add(currentNode);
            currentNode = currentNode.NextNode;
        }

        // Request contributions from all nodes simultaneously
        foreach (var node in nodes)
        {
            tasks.Add(node.ProvideContribution(globals));
        }

        var contributions = await Task.WhenAll(tasks);

        return string.Join("\r\n", contributions);
    }

    public async Task<string> ProvideContribution(ScriptGlobals globals)
    {
        try
        {
            var scriptOptions = ScriptOptions.Default
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(a => !string.IsNullOrEmpty(a.Location))) // Add default assemblies with location
                .WithReferences(await Task.WhenAll(_state.Packages.Select(PackageManager.LoadNuGetAssembly))) // Load packages
                .WithImports(_state.Imports) // Add imports
                .WithFilePath($"{_state.Name}.csx"); // Optional: set a file path for debugging

            // Create a cancellation token
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Compile and execute the code in _state with the options, globals, and cancellation token
            var result = await CSharpScript.EvaluateAsync(_state.Code, scriptOptions, globals, typeof(ScriptGlobals), cancellationToken);

            return $"Node {Info.Name} contributes: {result}";
        }
        catch (Exception ex)
        {
            // Handle compilation/execution errors
            Console.WriteLine($"{Info.Name} encountered an error in code execution: {ex.Message}\r\n{ex.InnerException?.Message}\r\n{ex.StackTrace}");
            return $"Node {Info.Name} encountered an error.";
        }
    }

    public void InsertNodeAfter(Node newNode)
    {
        // Insert the new node after the current node
        newNode.NextNode = this.NextNode;
        this.NextNode = newNode;
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
    private readonly Dictionary<string, object> _globalsData = [];

    public async Task<string> ProvideContributionWithWait(ScriptGlobals globals)
    {
        try
        {
            var scriptOptions = ScriptOptions.Default
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(a => !string.IsNullOrEmpty(a.Location))) // Add default assemblies with location
                .WithReferences(await Task.WhenAll(_state.Packages.Select(PackageManager.LoadNuGetAssembly))) // Load packages
                .WithImports(_state.Imports) // Add imports
                .WithFilePath($"{_state.Name}.csx"); // Optional: set a file path for debugging

            // Create a cancellation token
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Compile and execute the code in _state with the options, globals, and cancellation token
            var result = await CSharpScript.EvaluateAsync<string>(_state.Code, scriptOptions, globals, typeof(ScriptGlobals), cancellationToken);

            // Access the modified state
            Console.WriteLine($"Modified ExtraState: N/A");

            return $"Node {Info.Name} contributes: {result}";
        }
        catch (Exception ex)
        {
            // Handle compilation/execution errors
            Console.WriteLine($"{Info.Name} encountered an error in code execution: {ex.Message}\r\n{ex.InnerException?.Message}\r\n{ex.StackTrace}");
            return $"Node {Info.Name} encountered an error.";
        }
    }
}


public class Blob(Dictionary<RSAParameters, byte[]> compressedData)
{
    public readonly Dictionary<RSAParameters, byte[]> _compressedData = compressedData;

    public static Blob CreateCompressedBlob(Dictionary<RSAParameters, Script> data)
    {
        var compressedData = new Dictionary<RSAParameters, byte[]>();

        foreach (var (key, script) in data)
        {
            // Serialize the Script object to JSON
            var scriptJson = JsonSerializer.Serialize(script);
            var scriptBytes = Encoding.UTF8.GetBytes(scriptJson);

            using var rsa = RSA.Create();
            rsa.ImportParameters(key);
            var encryptedChunk = rsa.Encrypt(scriptBytes, RSAEncryptionPadding.Pkcs1);
            compressedData[key] = encryptedChunk;
        }

        return new Blob(compressedData);
    }

    public Script? ViewSilhouette(RSAParameters publicKey, RSA privateKey)
    {
        if (!_compressedData.ContainsKey(publicKey))
        {
            return null;
        }

        try
        {
            var encryptedChunk = _compressedData[publicKey];
            var decryptedBytes = privateKey.Decrypt(encryptedChunk, RSAEncryptionPadding.Pkcs1);
            var scriptJson = Encoding.UTF8.GetString(decryptedBytes);
            return JsonSerializer.Deserialize<Script>(scriptJson);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
