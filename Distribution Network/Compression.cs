using System;
using System.Text;

namespace Doorfail.Distribution.Network;

public class Compression
{
	// Dictionary mapping C# keywords to bytes with the highest bit set (1xxxxxxx)
	private static readonly Dictionary<string, byte> KeywordMap = new()
	{
		{ "abstract", 0x80 }, // 10000000
        { "as", 0x81 },       // 10000001
        { "base", 0x82 },     // 10000010
        { "bool", 0x83 },     // 10000011
        { "break", 0x84 },    // 10000100
        { "byte", 0x85 },     // 10000101
        { "case", 0x86 },     // 10000110
        { "catch", 0x87 },    // 10000111
        { "char", 0x88 },     // 10001000
        { "checked", 0x89 },  // 10001001
        { "class", 0x8A },    // 10001010
        { "const", 0x8B },    // 10001011
        { "continue", 0x8C }, // 10001100
        { "decimal", 0x8D },  // 10001101
        { "default", 0x8E },  // 10001110
        { "delegate", 0x8F }, // 10001111
        { "do", 0x90 },       // 10010000
        { "double", 0x91 },   // 10010001
        { "else", 0x92 },     // 10010010
        { "enum", 0x93 },     // 10010011
        { "event", 0x94 },    // 10010100
        { "explicit", 0x95 }, // 10010101
        { "extern", 0x96 },   // 10010110
        { "false", 0x97 },    // 10010111
        { "finally", 0x98 },  // 10011000
        { "fixed", 0x99 },    // 10011001
        { "float", 0x9A },    // 10011010
        { "for", 0x9B },      // 10011011
        { "foreach", 0x9C },  // 10011100
        { "goto", 0x9D },     // 10011101
        { "if", 0x9E },       // 10011110
        { "implicit", 0x9F }, // 10011111
        { "in", 0xA0 },       // 10100000
        { "int", 0xA1 },      // 10100001
        { "interface", 0xA2 },// 10100010
        { "internal", 0xA3 }, // 10100011
        { "is", 0xA4 },       // 10100100
        { "lock", 0xA5 },     // 10100101
        { "long", 0xA6 },     // 10100110
        { "namespace", 0xA7 },// 10100111
        { "new", 0xA8 },      // 10101000
        { "null", 0xA9 },     // 10101001
        { "object", 0xAA },   // 10101010
        { "operator", 0xAB }, // 10101011
        { "out", 0xAC },      // 10101100
        { "override", 0xAD }, // 10101101
        { "params", 0xAE },   // 10101110
        { "private", 0xAF },  // 10101111
        { "protected", 0xB0 },// 10110000
        { "public", 0xB1 },   // 10110001
        { "readonly", 0xB2 }, // 10110010
        { "ref", 0xB3 },      // 10110011
        { "return", 0xB4 },   // 10110100
        { "sbyte", 0xB5 },    // 10110101
        { "sealed", 0xB6 },   // 10110110
        { "short", 0xB7 },    // 10110111
        { "sizeof", 0xB8 },   // 10111000
        { "set", 0xB9 },      // 10111001
        { "static", 0xBA },   // 10111010
        { "string", 0xBB },   // 10111011
        { "struct", 0xBC },   // 10111100
        { "switch", 0xBD },   // 10111101
        { "this", 0xBE },     // 10111110
        { "throw", 0xBF },    // 10111111
        { "true", 0xC0 },     // 11000000
        { "try", 0xC1 },      // 11000001
        { "typeof", 0xC2 },   // 11000010
        { "uint", 0xC3 },     // 11000011
        { "ulong", 0xC4 },    // 11000100
        { "value", 0xC5 },    // 11000101
        { "unsafe", 0xC6 },   // 11000110
        { "ushort", 0xC7 },   // 11000111
        { "using", 0xC8 },    // 11001000
        { "virtual", 0xC9 },  // 11001001
        { "void", 0xCA },     // 11001010
        { "get", 0xCB },      // 11001011
        { "while", 0xCC },    // 11001100
        { "[]", 0xCD },       // 11001101
        { "//", 0xCE }        // 11001110
    };

	public static byte[] Compress(string input, Guid guid)
	{
		int offset = CalculateOffset(guid);
		var keywordMap = GetRandomizedKeywordMap(guid);

		var segments = SplitWithWhitespace(input);

		List<byte> output = new(input.Length);
		foreach (var segment in segments)
		{
			if (char.IsWhiteSpace(segment[0]))
			{
				foreach (char c in segment)
				{
					output.Add((byte)c); // Directly add whitespace characters
				}

				continue;
			}

			if (keywordMap.TryGetValue(segment, out byte value))
			{
				output.Add((byte)(value + offset)); // Add the byte with the offset
				continue;
			}

			foreach (char c in segment)
			{
				output.Add((byte)((c & 0x7F) + offset)); // Ensure highest bit is 0 (0xxxxxxx) and add offset
			}
		}

		return [.. output];
	}


	public static string Decompress(Guid guid, byte[] input)
	{
		int offset = CalculateOffset(guid);
		var keywordMap = GetRandomizedKeywordMap(guid);

		var output = new StringBuilder();
		var dataBytes = input.Skip(16);

		foreach (var b in dataBytes)
		{
			if (char.IsWhiteSpace((char)b))
			{
				output.Append((char)b); // Directly append whitespace characters
				continue;
			}

			var adjustedByte = (byte)(b - offset);
			var keyword = keywordMap.FirstOrDefault(m => m.Value == adjustedByte).Key;
			if (keyword is not null)
			{
				output.Append(keyword); // Append keyword
				continue;
			}
			output.Append((char)(adjustedByte & 0x7F)); // Convert byte to char
		}

		return output.ToString();
	}


	private static int CalculateOffset(Guid guid)
	{
		var random = new Random(BitConverter.ToInt32(guid.ToByteArray(), 0));
		return guid.ToByteArray().Sum(_ => random.Next()) % 128;
	}

	private static Dictionary<string, byte> GetRandomizedKeywordMap(Guid guid)
	{
		var random = new Random(BitConverter.ToInt32(guid.ToByteArray(), 0));
		return KeywordMap.OrderBy(_ => random.Next()).ToDictionary(item => item.Key, item => item.Value);
	}

	private static List<string> SplitWithWhitespace(string input)
	{
		var result = new List<string>();
		var currentWord = new StringBuilder();
		var currentWhitespace = new StringBuilder();

		foreach (char c in input)
		{
			if (char.IsWhiteSpace(c))
			{
				if (currentWord.Length > 0)
				{
					result.Add(currentWord.ToString());
					currentWord.Clear();
				}
				currentWhitespace.Append(c);
			}
			else
			{
				if (currentWhitespace.Length > 0)
				{
					result.Add(currentWhitespace.ToString());
					currentWhitespace.Clear();
				}
				currentWord.Append(c);
			}
		}

		if (currentWord.Length > 0)
		{
			result.Add(currentWord.ToString());
		}
		if (currentWhitespace.Length > 0)
		{
			result.Add(currentWhitespace.ToString());
		}

		return result;
	}
}