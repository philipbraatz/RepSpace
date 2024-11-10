using System.Text;
using System.Text.Json;

namespace Doorfail.Distribution.Network;

public class Blob
{
    public Dictionary<Guid, byte[]> CompressedData { get; set; } = new Dictionary<Guid, byte[]>();

    public Blob() { }
    public Blob(Dictionary<Guid, byte[]> compressedData)
    {
        CompressedData = compressedData;
    }

    public static Blob CreateCompressedBlob(Dictionary<Guid, Script> data)
    {
        var compressedData = new Dictionary<Guid, byte[]>();

        foreach (var (key, script) in data)
        {
            // Serialize the Script object to JSON
            var scriptJson = JsonSerializer.Serialize(script);
            var scriptBytes = Encoding.UTF8.GetBytes(scriptJson);

            // Compress the script bytes using the Node.Id (Guid)
            var compressedBytes = Compression.Compress(scriptJson, key);
            compressedData[key] = compressedBytes;
        }

        return new Blob(compressedData);
    }

    public Script? ViewSilhouette(Guid nodeId)
    {
        if (!CompressedData.ContainsKey(nodeId))
        {
            return null;
        }

        return ConsomeSilhouette(nodeId, CompressedData[nodeId]);
    }

    public Script? ConsomeSilhouette(Guid nodeId, byte[] bytes)
    {
        try
        {
            var decompressedJson = Compression.Decompress(nodeId, bytes);
            return JsonSerializer.Deserialize<Script>(decompressedJson);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
