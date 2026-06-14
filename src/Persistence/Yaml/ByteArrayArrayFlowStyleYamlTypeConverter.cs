using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
	#pragma warning disable CA1720 // Identifier contains type name
	public enum ByteArrayValueFormat
	{
		Decimal,
		Binary,
		Hexadecimal
	}
	#pragma warning restore CA1720

	/// <summary>
	/// Alternative YAML converter for byte[][] that produces true nested arrays in YAML.
	/// Instead of converting to strings like "[0, 10, 20]", this produces real YAML arrays.
	/// 
	/// Output example:
	/// LandValues:
	///   - [0, 10, 20, 30, 40]
	///   - [10, 20, 30, 40, 50]
	/// 
	/// This works by converting byte[][] to a List<List<byte>> which YamlDotNet
	/// serializes with the correct flow style for inner arrays.
	/// 
	/// It is possible to use a different format for byte values (decimal, binary or hexadecimal) by specifying the ByteArrayValueFormat in the constructor.
	/// </summary>
	public class ByteArrayArrayFlowStyleYamlTypeConverter : IYamlTypeConverter
	{
		private readonly ByteArrayValueFormat _valueFormat;

		public ByteArrayArrayFlowStyleYamlTypeConverter(ByteArrayValueFormat valueFormat = ByteArrayValueFormat.Decimal)
		{
			_valueFormat = valueFormat;
		}

		public bool Accepts(Type type) => type == typeof(byte[][]);

		public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
		{
			// Deserialize as List<List<object>> to support decimal, binary and hexadecimal string formats.
			List<List<object>>? listOfLists = (List<List<object>>?)rootDeserializer(typeof(List<List<object>>));

			if (listOfLists == null)
				return null;

			var result = new byte[listOfLists.Count][];
			for (int i = 0; i < listOfLists.Count; i++)
			{
				if (listOfLists[i] != null)
				{
					result[i] = new byte[listOfLists[i].Count];
					for (int j = 0; j < listOfLists[i].Count; j++)
					{
						result[i][j] = ParseByteValue(listOfLists[i][j]);
					}
				}
			}

			return result;
		}

		private static byte ParseByteValue(object value)
		{
			if (value is byte byteValue)
			{
				return byteValue;
			}

			if (value == null)
			{
				throw new FormatException("Byte value cannot be null.");
			}

			var token = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
			if (string.IsNullOrWhiteSpace(token))
			{
				throw new FormatException("Byte value cannot be empty.");
			}

			if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				return byte.Parse(token[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
			}

			if (token.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
			{
				return Convert.ToByte(token[2..], 2);
			}

			if (token.Length == 8 && token.All(c => c == '0' || c == '1'))
			{
				return Convert.ToByte(token, 2);
			}

			if (byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalValue))
			{
				return decimalValue;
			}

			if (token.Any(c => c is >= 'A' and <= 'F' || c is >= 'a' and <= 'f'))
			{
				return byte.Parse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
			}

			throw new FormatException($"Invalid byte value '{token}'.");
		}

		public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
		{
			if (value == null)
			{
				emitter.Emit(new Scalar("null"));
				return;
			}
			var data = (byte[][])value;

			// Outer array (block style)
			emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

			foreach (var row in data)
			{
				// Inner arrays (flow style → [1, 2, 3])
				emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));

				foreach (var b in row)
				{
					emitter.Emit(new Scalar(FormatByteValue(b)));
				}

				emitter.Emit(new SequenceEnd());
			}

			emitter.Emit(new SequenceEnd());
		}

		private string FormatByteValue(byte value)
			=> _valueFormat switch
			{
				ByteArrayValueFormat.Binary => Convert.ToString(value, 2).PadLeft(8, '0'),
				ByteArrayValueFormat.Hexadecimal => value.ToString("X2", CultureInfo.InvariantCulture),
				_ => value.ToString(CultureInfo.InvariantCulture)
			};
	}
}
