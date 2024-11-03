using System.Text;
using System.Text.Json;

namespace Doorfail.Distribution.Network;

public class Blob
{
	public readonly Dictionary<Guid, byte[]> _compressedData;

	public Blob(Dictionary<Guid, byte[]> compressedData)
	{
		_compressedData = compressedData;
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
		if (!_compressedData.ContainsKey(nodeId))
		{
			return null;
		}

		return ConsomeSilhouette(nodeId, _compressedData[nodeId]);
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
